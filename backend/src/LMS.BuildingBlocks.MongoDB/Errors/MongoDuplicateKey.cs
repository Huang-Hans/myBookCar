using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Errors;

public static class MongoDuplicateKey
{
    private const int DuplicateKeyErrorCode = 11000;

    public static bool IsDuplicateKey(Exception exception) => exception switch
    {
        MongoWriteException writeException => writeException.WriteError.Category == ServerErrorCategory.DuplicateKey,
        MongoCommandException commandException => commandException.Code == DuplicateKeyErrorCode,
        _ => false
    };
}
