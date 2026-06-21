using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace StartupConnect.Infrastructure.Email;

public sealed class EmailOutboxProtector(IOptions<EmailOutboxOptions> optionsAccessor)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(
        optionsAccessor.Value.EncryptionKey.Length >= 32
            ? optionsAccessor.Value.EncryptionKey
            : throw new InvalidOperationException("Email:Outbox:EncryptionKey must be at least 32 characters.")));

    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        ciphertext.CopyTo(payload, NonceSize + TagSize);
        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedPayload)
    {
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(protectedPayload);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Email outbox payload is invalid.", exception);
        }

        if (payload.Length <= NonceSize + TagSize)
        {
            throw new InvalidOperationException("Email outbox payload is invalid.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (AuthenticationTagMismatchException exception)
        {
            throw new InvalidOperationException("Email outbox payload authentication failed.", exception);
        }
    }
}
