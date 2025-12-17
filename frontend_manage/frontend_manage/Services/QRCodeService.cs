using QRCoder;

namespace frontend_manage.Services
{
    public interface IQrCodeService
    {
        string GenerateQrCodeAsBase64(string text, int pixelsPerModule = 5);
    }

    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrCodeAsBase64(string text, int pixelsPerModule = 5)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
                    return Convert.ToBase64String(qrCodeBytes);
                }
            }
        }
    }
}

