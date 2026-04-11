using BE_AI_Tourism.Application.DTOs.Community;
using BE_AI_Tourism.Configuration;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Infrastructure.Cloudinary;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Application.Services.Community;

public class CommunityService : ICommunityService
{
    private const string PublicGroupSlug = "public";

    private readonly IRepository<Domain.Entities.CommunityGroup> _groupRepository;
    private readonly IRepository<Domain.Entities.CommunityPost> _postRepository;
    private readonly IRepository<Domain.Entities.CommunityPostMedia> _postMediaRepository;
    private readonly IRepository<Domain.Entities.CommunityComment> _commentRepository;
    private readonly IRepository<Domain.Entities.CommunityReaction> _reactionRepository;
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly ICloudinaryProvider _cloudinaryProvider;
    private readonly CloudinaryOptions _cloudinaryOptions;
    private readonly IMapper _mapper;

    public CommunityService(
        IRepository<Domain.Entities.CommunityGroup> groupRepository,
        IRepository<Domain.Entities.CommunityPost> postRepository,
        IRepository<Domain.Entities.CommunityPostMedia> postMediaRepository,
        IRepository<Domain.Entities.CommunityComment> commentRepository,
        IRepository<Domain.Entities.CommunityReaction> reactionRepository,
        IRepository<Domain.Entities.User> userRepository,
        ICloudinaryProvider cloudinaryProvider,
        IOptions<CloudinaryOptions> cloudinaryOptions,
        IMapper mapper)
    {
        _groupRepository = groupRepository;
        _postRepository = postRepository;
        _postMediaRepository = postMediaRepository;
        _commentRepository = commentRepository;
        _reactionRepository = reactionRepository;
        _userRepository = userRepository;
        _cloudinaryProvider = cloudinaryProvider;
        _cloudinaryOptions = cloudinaryOptions.Value;
        _mapper = mapper;
    }

    public async Task<Result<CommunityGroupResponse>> GetPublicGroupAsync()
    {
        var group = await _groupRepository.FindOneAsync(g => g.Slug == PublicGroupSlug && g.IsPublic && g.IsActive);
        if (group == null)
            return Result.Fail<CommunityGroupResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        return Result.Ok(_mapper.Map<CommunityGroupResponse>(group));
    }

    public async Task<Result<PaginationResponse<CommunityPostResponse>>> GetPublicGroupPostsAsync(PaginationRequest request)
    {
        var group = await _groupRepository.FindOneAsync(g => g.Slug == PublicGroupSlug && g.IsPublic && g.IsActive);
        if (group == null)
            return Result.Fail<PaginationResponse<CommunityPostResponse>>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var allPosts = await _postRepository.FindAsync(p => p.GroupId == group.Id);
        var ordered = allPosts.OrderByDescending(p => p.CreatedAt).ToList();
        var items = ordered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = await BuildPostResponsesAsync(items, includeComments: false);

        return Result.Ok(PaginationResponse<CommunityPostResponse>.Create(
            responses, ordered.Count, request.PageNumber, request.PageSize));
    }

    public async Task<Result<CommunityPostResponse>> CreatePublicPostAsync(CreateCommunityPostRequest request, Guid userId)
    {
        var group = await _groupRepository.FindOneAsync(g => g.Slug == PublicGroupSlug && g.IsPublic && g.IsActive);
        if (group == null)
            return Result.Fail<CommunityPostResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var post = new Domain.Entities.CommunityPost
        {
            GroupId = group.Id,
            UserId = userId,
            Content = request.Content.Trim()
        };

        await _postRepository.AddAsync(post);

        var response = (await BuildPostResponsesAsync([post], includeComments: false)).First();
        return Result.Ok(response, StatusCodes.Status201Created);
    }

    public async Task<Result<CommunityPostResponse>> GetPostByIdAsync(Guid postId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return Result.Fail<CommunityPostResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var response = (await BuildPostResponsesAsync([post], includeComments: true)).First();
        return Result.Ok(response);
    }

    public async Task<Result<CommunityCommentResponse>> AddCommentAsync(Guid postId, AddCommunityCommentRequest request, Guid userId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return Result.Fail<CommunityCommentResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var comment = new Domain.Entities.CommunityComment
        {
            PostId = postId,
            UserId = userId,
            Content = request.Content.Trim()
        };

        await _commentRepository.AddAsync(comment);

        var user = await _userRepository.GetByIdAsync(userId);
        var response = _mapper.Map<CommunityCommentResponse>(comment);
        if (user != null)
        {
            response.UserFullName = user.FullName;
            response.UserAvatarUrl = user.AvatarUrl;
        }

        return Result.Ok(response, StatusCodes.Status201Created);
    }

    public async Task<Result<CommunityPostResponse>> ReactAsync(Guid postId, ReactCommunityPostRequest request, Guid userId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return Result.Fail<CommunityPostResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var normalizedReactionType = request.ReactionType.Trim().ToLowerInvariant();
        var existing = await _reactionRepository.FindOneAsync(r => r.PostId == postId && r.UserId == userId);

        if (existing == null)
        {
            await _reactionRepository.AddAsync(new Domain.Entities.CommunityReaction
            {
                PostId = postId,
                UserId = userId,
                ReactionType = normalizedReactionType
            });
        }
        else if (existing.ReactionType == normalizedReactionType)
        {
            await _reactionRepository.DeleteAsync(existing.Id);
        }
        else
        {
            existing.ReactionType = normalizedReactionType;
            await _reactionRepository.UpdateAsync(existing);
        }

        var response = (await BuildPostResponsesAsync([post], includeComments: false)).First();
        return Result.Ok(response);
    }

    public async Task<Result<CommunityPostUploadSignatureResponse>> GeneratePostUploadSignatureAsync(CommunityPostUploadSignatureRequest request, Guid userId)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId);
        if (post == null)
            return Result.Fail<CommunityPostUploadSignatureResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (post.UserId != userId)
            return Result.Fail<CommunityPostUploadSignatureResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        var folder = $"{_cloudinaryOptions.Folder}/community/posts/{request.PostId}";
        var (signature, timestamp) = _cloudinaryProvider.GenerateSignature(folder);

        return Result.Ok(new CommunityPostUploadSignatureResponse
        {
            Signature = signature,
            Timestamp = timestamp,
            ApiKey = _cloudinaryOptions.ApiKey,
            CloudName = _cloudinaryOptions.CloudName,
            Folder = folder
        });
    }

    public async Task<Result<CommunityPostMediaResponse>> FinalizePostMediaAsync(FinalizeCommunityPostMediaRequest request, Guid userId)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId);
        if (post == null)
            return Result.Fail<CommunityPostMediaResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (post.UserId != userId)
            return Result.Fail<CommunityPostMediaResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        var existingMedia = await _postMediaRepository.FindAsync(m => m.PostId == request.PostId);

        var media = new Domain.Entities.CommunityPostMedia
        {
            PostId = request.PostId,
            Url = request.Url.Trim(),
            SecureUrl = request.SecureUrl.Trim(),
            PublicId = request.PublicId.Trim(),
            Format = request.Format.Trim(),
            MimeType = request.MimeType.Trim(),
            Bytes = request.Bytes,
            Width = request.Width,
            Height = request.Height,
            SortOrder = existingMedia.Count()
        };

        await _postMediaRepository.AddAsync(media);
        return Result.Ok(_mapper.Map<CommunityPostMediaResponse>(media), StatusCodes.Status201Created);
    }

    private async Task<List<CommunityPostResponse>> BuildPostResponsesAsync(List<Domain.Entities.CommunityPost> posts, bool includeComments)
    {
        if (posts.Count == 0)
            return [];

        var postIds = posts.Select(p => p.Id).ToList();
        var userIds = posts.Select(p => p.UserId).Distinct().ToList();

        var users = await _userRepository.FindAsync(u => userIds.Contains(u.Id));
        var userById = users.ToDictionary(u => u.Id, u => u);

        var allMedia = await _postMediaRepository.FindAsync(m => postIds.Contains(m.PostId));
        var mediaByPost = allMedia
            .OrderBy(m => m.SortOrder)
            .GroupBy(m => m.PostId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allReactions = await _reactionRepository.FindAsync(r => postIds.Contains(r.PostId));
        var reactionCountByPost = allReactions
            .GroupBy(r => r.PostId)
            .ToDictionary(g => g.Key, g => g.Count());

        var allComments = await _commentRepository.FindAsync(c => postIds.Contains(c.PostId));
        var commentCountByPost = allComments
            .GroupBy(c => c.PostId)
            .ToDictionary(g => g.Key, g => g.Count());

        var commentUserIds = allComments.Select(c => c.UserId).Distinct().ToList();
        var missingCommentUserIds = commentUserIds.Where(id => !userById.ContainsKey(id)).ToList();
        if (missingCommentUserIds.Count > 0)
        {
            var missingUsers = await _userRepository.FindAsync(u => missingCommentUserIds.Contains(u.Id));
            foreach (var user in missingUsers)
                userById[user.Id] = user;
        }

        var commentsByPost = allComments
            .OrderBy(c => c.CreatedAt)
            .GroupBy(c => c.PostId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var responses = new List<CommunityPostResponse>(posts.Count);
        foreach (var post in posts)
        {
            var response = _mapper.Map<CommunityPostResponse>(post);
            if (userById.TryGetValue(post.UserId, out var user))
            {
                response.UserFullName = user.FullName;
                response.UserAvatarUrl = user.AvatarUrl;
            }

            response.ReactionCount = reactionCountByPost.GetValueOrDefault(post.Id, 0);
            response.CommentCount = commentCountByPost.GetValueOrDefault(post.Id, 0);

            if (mediaByPost.TryGetValue(post.Id, out var media))
                response.Media = media.Select(m => _mapper.Map<CommunityPostMediaResponse>(m)).ToList();

            if (includeComments && commentsByPost.TryGetValue(post.Id, out var comments))
            {
                response.Comments = comments.Select(c =>
                {
                    var commentResponse = _mapper.Map<CommunityCommentResponse>(c);
                    if (userById.TryGetValue(c.UserId, out var commentUser))
                    {
                        commentResponse.UserFullName = commentUser.FullName;
                        commentResponse.UserAvatarUrl = commentUser.AvatarUrl;
                    }

                    return commentResponse;
                }).ToList();
            }

            responses.Add(response);
        }

        return responses;
    }
}
