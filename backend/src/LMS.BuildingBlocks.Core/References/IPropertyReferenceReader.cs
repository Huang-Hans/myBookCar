namespace LMS.BuildingBlocks.Core.References;

public interface IPropertyReferenceReader
{
    Task<bool> HasReferencesAsync(string propertyId, CancellationToken cancellationToken);
}
