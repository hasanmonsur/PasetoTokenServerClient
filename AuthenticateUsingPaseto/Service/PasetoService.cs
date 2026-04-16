using Paseto;
using Paseto.Builder;
using Paseto.Cryptography.Key;
using Paseto.Protocol;



namespace WebApiPaseto.Service
{
    public class PasetoService
    {
        private readonly PasetoSymmetricKey _symmetricKey;
        public PasetoService(
            IConfiguration configuration
            )
        {
            _symmetricKey = new PasetoSymmetricKey(
                Convert.FromBase64String(configuration["Paseto:SymmetricKey"] ?? throw new InvalidOperationException("Paseto:SymmetricKey configuration is missing")),
                new Version4()
            );

        }
        public string GenerateToken(string userId, string email)
        {
            var token = new PasetoBuilder()
                .Use(ProtocolVersion.V4, Purpose.Local)
                .WithKey(_symmetricKey)
                .Subject(userId)
                .AddClaim("email", email)
                .AddClaim("role", "customer")
                .IssuedAt(DateTime.UtcNow)
                .Expiration(DateTime.UtcNow.AddHours(1))
                .Encode();

            return token;
        }

        //VALIDATE TOKEN
        public PasetoTokenValidationResult ValidateToken(string token)
        {
            var validationParameters = new PasetoTokenValidationParameters
            {
                ValidateLifetime = true
            };

            var result = new PasetoBuilder()
                .Use(ProtocolVersion.V4, Purpose.Local)
                .WithKey(_symmetricKey)
                .Decode(token, validationParameters);

            return result;
        }
    }
}
