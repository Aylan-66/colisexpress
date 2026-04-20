using ColisExpress.Application.Interfaces;
using QRCoder;

namespace ColisExpress.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateBase64Png(string data)
    {
        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var bytes = qrCode.GetGraphic(10);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
