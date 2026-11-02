namespace LMS.Booking.Application.Directory;

public interface IDriverContactDirectory
{
    Task<DirectoryResult<string>> GetPhoneNumberAsync(string driverId, CancellationToken cancellationToken);
}
