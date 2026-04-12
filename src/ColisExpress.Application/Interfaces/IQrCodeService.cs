namespace ColisExpress.Application.Interfaces;

public interface IQrCodeService
{
    string GenerateBase64Png(string data);
}
