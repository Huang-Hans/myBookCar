namespace LMS.Platform.Domain.Files;

// 跨境审批的四种材料类型。对外字面值为 PASSENGER_TRAVEL_DOCUMENT / VEHICLE_PERMIT / DRIVER_LICENSE / CUSTOMS_DECLARATION。
public enum FileDocumentType
{
    PassengerTravelDocument,
    VehiclePermit,
    DriverLicense,
    CustomsDeclaration
}
