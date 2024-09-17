using System.Security.Cryptography;
using System.Text;

namespace Permission_Based_Authorization.Services
{
    public interface IHasherService
    {
        string Salt(int size = 32);

        string Hasher(string value, string salt);
    }

    public class HasherService : IHasherService
    {
        [Obsolete]
        public string Salt(int size = 32)
        {
            byte[] buff = new byte[size];
            using RNGCryptoServiceProvider rng = new();
            rng.GetBytes(buff);
            return Convert.ToBase64String(buff);
        }

        public string Hasher(string value, string salt)
        {
            using var alg = new HMACSHA256(GetBytes(salt));
            var result = alg.ComputeHash(GetBytes(value));
            return Convert.ToBase64String(result);
        }

        private static byte[] GetBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
}
