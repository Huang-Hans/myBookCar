namespace LMS.Platform.Application.Configuration.Ports;

public interface IPropertyReferenceDirectory
{
    // Null means an authoritative service could not be checked.
    Task<bool?> HasReferencesAsync(string propertyId, CancellationToken cancellationToken);
}
