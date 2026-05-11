using FirebaseAdmin.Auth;

namespace ObjetosIngresos.Services
{
    public class AuthServices
    {
        public async Task<string> RegistrarEnFirebase(string email, string password)
        {
            try
            {
                var args = new UserRecordArgs()
                {
                    Email = email,
                    Password = password,
                };
                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);

                return userRecord.Uid;
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
                {
                    throw new Exception("El usuario ya está registrado en la plataforma de autenticación.");
                }

                throw new Exception($"Error de Firebase: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado al registrar: {ex.Message}");
            }
        }
    }
}
