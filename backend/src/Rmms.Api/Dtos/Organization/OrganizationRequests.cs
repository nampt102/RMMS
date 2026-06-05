namespace Rmms.Api.Dtos.Organization;

// ----- Stores -----
public sealed record CreateStoreRequest(string Code, string Name, string? Address, decimal Latitude, decimal Longitude, Guid? AreaId);
public sealed record UpdateStoreRequest(string Name, string? Address, decimal Latitude, decimal Longitude, Guid? AreaId);
public sealed record ChangeStoreStatusRequest(string Status);

// ----- Areas -----
public sealed record CreateAreaRequest(string Code, string Name, Guid? ParentAreaId);
public sealed record UpdateAreaRequest(string Name, Guid? ParentAreaId);

// ----- Categories -----
public sealed record CreateCategoryRequest(string Code, string Name);
public sealed record UpdateCategoryRequest(string Name);

// ----- Assignments -----
public sealed record AssignPgLeaderRequest(Guid PgUserId, Guid LeaderUserId);
public sealed record AssignUserStoreRequest(Guid UserId, Guid StoreId);
public sealed record AssignUserCategoryRequest(Guid UserId, Guid CategoryId);
