namespace LMS.Booking.Domain.Approvals;

// 四种规定材料。对外字面值为 PASSENGER_TRAVEL_DOCUMENT / VEHICLE_PERMIT / DRIVER_LICENSE / CUSTOMS_DECLARATION。
public enum ApprovalDocumentType
{
    PassengerTravelDocument,
    VehiclePermit,
    DriverLicense,
    CustomsDeclaration
}
