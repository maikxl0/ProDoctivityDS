using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ProDoctivityDS.Shared.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger )
        {
            // El "propósito" actúa como una clave secundaria; asegúrate de que sea único para este módulo.
            // Si cambias este propósito, los datos previamente cifrados NO podrán descifrarse.
            _protector = provider.CreateProtector("Prodoctivity.Encryption.v1");
            _logger = logger;
        }

        /// <summary>
        /// Cifra un texto plano.
        /// </summary>
        /// <param name="plainText">Texto a cifrar (puede ser null o vacío).</param>
        /// <returns>Texto cifrado en Base64.</returns>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                byte[] protectedBytes = _protector.Protect(plainBytes);
                return Convert.ToBase64String(protectedBytes);
            }
            catch (Exception ex)
            {
                // Loggear el error (puedes inyectar ILogger si lo deseas)
                throw new InvalidOperationException("Error al cifrar el texto.", ex);
            }
        }

        /// <summary>
        /// Descifra un texto previamente cifrado con Encrypt.
        /// </summary>
        /// <param name="cipherText">Texto cifrado en Base64.</param>
        /// <returns>Texto plano original.</returns>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = _protector.Unprotect(protectedBytes);
                string plainText = Encoding.UTF8.GetString(plainBytes);
                _logger.LogDebug("Decrypt OK: longitud cifrado={0}, longitud resultado={1}", cipherText.Length, plainText.Length);
                return plainText;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Error criptográfico al descifrar (posible cambio de clave o datos corruptos)");
                throw new InvalidOperationException("Error de descifrado: clave incorrecta o datos dañados", ex);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "El texto cifrado no es Base64 válido: {CipherTextPreview}", cipherText[..Math.Min(50, cipherText.Length)]);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en Decrypt");
                throw;
            }
        }
    }
    
}

