namespace SessionManagement.Application.Common;

internal static class TeamRequestorValidation
{
    public static void EnsureRequestorId(Guid requestorId)
    {
        if (requestorId == Guid.Empty)
            throw new ArgumentException("El parámetro requestorId es obligatorio.");
    }
}
