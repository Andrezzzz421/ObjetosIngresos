namespace ObjetosIngresos.Helpers
{
    public class APHelpers
    {
        public static byte[] ToBytes(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static string ToBase64(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return string.Empty;

            string base64String = Convert.ToBase64String(imageBytes);

            return $"data:image/jpeg;base64,{base64String}";
        }
    }
}
