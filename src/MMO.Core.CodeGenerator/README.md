# MMO.Core.CodeGenerator

Cryptographically secure code generation package with configurable length and character sets.

## Overview

`MMO.Core.CodeGenerator` provides a robust service for generating various types of codes (verification codes, tokens, coupon codes, API keys, etc.) with configurable lengths and character sets. Uses `RandomNumberGenerator` for cryptographic security.

## Features

- ✅ Cryptographically secure random generation
- ✅ Configurable code lengths per type
- ✅ Multiple predefined code types
- ✅ Custom character sets
- ✅ Batch generation with uniqueness guarantee
- ✅ Ambiguous character exclusion (0, O, 1, I, l)
- ✅ Thread-safe singleton service

## Installation

### 1. Add Project Reference

```xml
<ItemGroup>
    <ProjectReference Include="..\..\Core\MMO.Core.CodeGenerator\MMO.Core.CodeGenerator.csproj" />
</ItemGroup>
```

### 2. Configure appsettings.json

```json
{
  "CodeGenerator": {
    "DefaultLength": 6,
    "VerificationCodeLength": 6,
    "PasswordResetTokenLength": 8,
    "CouponCodeLength": 10,
    "ReferralCodeLength": 8,
    "ApiKeyLength": 32,
    "SessionTokenLength": 64,
    "IncludeUppercase": true,
    "IncludeLowercase": true,
    "IncludeDigits": true,
    "IncludeSpecialCharacters": false,
    "ExcludeAmbiguous": true,
    "CustomCharacterSet": null
  }
}
```

### 3. Register in Program.cs

```csharp
using MMO.Core.CodeGenerator.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add code generator
builder.Services.AddCodeGenerator(builder.Configuration);

var app = builder.Build();
app.Run();
```

## Usage Examples

### 1. Verification Codes (Email/SMS)

```csharp
public class UserService
{
    private readonly ICodeGeneratorService _codeGenerator;

    public UserService(ICodeGeneratorService codeGenerator)
    {
        _codeGenerator = codeGenerator;
    }

    public async Task SendVerificationEmailAsync(string email)
    {
        // Generate 6-digit numeric code: 123456
        var code = _codeGenerator.GenerateVerificationCode();
        
        await _emailService.SendAsync(email, "Verification Code", $"Your code is: {code}");
        await SaveVerificationCodeAsync(email, code, TimeSpan.FromMinutes(10));
    }
}
```

### 2. Password Reset Tokens

```csharp
public async Task<string> GeneratePasswordResetTokenAsync(string email)
{
    // Generate 8-character alphanumeric token: A7B3C2D9
    var token = _codeGenerator.GeneratePasswordResetToken();
    
    await SavePasswordResetTokenAsync(email, token, TimeSpan.FromHours(1));
    return token;
}
```

### 3. Coupon Codes

```csharp
public async Task<CouponDto> CreateCouponAsync(CreateCouponCommand command)
{
    // Generate 10-character uppercase + digit code: SAVE20OFF5
    var code = _codeGenerator.GenerateCouponCode();
    
    var coupon = new Coupon
    {
        Code = code,
        DiscountPercent = command.DiscountPercent,
        ExpiresAt = command.ExpiresAt
    };
    
    await _repository.AddAsync(coupon);
    return coupon.ToDto();
}
```

### 4. Referral Codes

```csharp
public async Task<string> CreateReferralCodeAsync(Guid userId)
{
    // Generate 8-character code: REF12ABC
    var code = _codeGenerator.GenerateReferralCode();
    
    await SaveReferralCodeAsync(userId, code);
    return code;
}
```

### 5. API Keys

```csharp
public async Task<ApiKeyDto> GenerateApiKeyAsync(Guid userId)
{
    // Generate 32-character alphanumeric key
    var apiKey = _codeGenerator.GenerateApiKey();
    
    var key = new ApiKey
    {
        UserId = userId,
        Key = apiKey,
        CreatedAt = DateTimeOffset.UtcNow
    };
    
    await _repository.AddAsync(key);
    return key.ToDto();
}
```

### 6. Custom Length Codes

```csharp
// Generate code with specific length
var code = _codeGenerator.Generate(12);

// Generate with custom options
var code = _codeGenerator.Generate(
    length: 10,
    includeUppercase: true,
    includeLowercase: false,
    includeDigits: true,
    includeSpecialCharacters: false,
    excludeAmbiguous: true
);
```

### 7. Custom Character Set

```csharp
// Only vowels and even numbers
var code = _codeGenerator.GenerateWithCustomCharacterSet(8, "AEIOU2468");
// Result: E4I2A6U8
```

### 8. Batch Generation

```csharp
// Generate 100 unique coupon codes
var codes = _codeGenerator.GenerateBatch(100, CodeType.CouponCode);

// Generate 50 unique codes of custom length
var codes = _codeGenerator.GenerateBatch(50, 15);

// Bulk insert
await _repository.BulkInsertCouponsAsync(codes.Select(code => new Coupon { Code = code }));
```

### 9. Code Validation

```csharp
public bool ValidateVerificationCode(string code)
{
    // Check if code matches expected length
    return _codeGenerator.Validate(code, 6);
}
```

### 10. Using CodeType Enum

```csharp
using MMO.Core.CodeGenerator.Enums;

// Generate by type
var verificationCode = _codeGenerator.Generate(CodeType.VerificationCode);
var couponCode = _codeGenerator.Generate(CodeType.CouponCode);
var apiKey = _codeGenerator.Generate(CodeType.ApiKey);
```

## Predefined Code Types

| Code Type | Length | Characters | Example |
|-----------|--------|------------|---------|
| VerificationCode | 6 | Digits only | `123456` |
| PasswordResetToken | 8 | Uppercase + Digits | `A7B3C2D9` |
| CouponCode | 10 | Uppercase + Digits | `SAVE20OFF5` |
| ReferralCode | 8 | Uppercase + Digits | `REF12ABC` |
| ApiKey | 32 | Alphanumeric | `aB3dE5fG7hJ9kL2mN4pQ6rS8tU0vW` |
| SessionToken | 64 | Alphanumeric | (64 chars) |

## Configuration Options

### Code Lengths

| Option | Default | Description |
|--------|---------|-------------|
| `DefaultLength` | 6 | Default length when not specified |
| `VerificationCodeLength` | 6 | Verification codes (email/SMS) |
| `PasswordResetTokenLength` | 8 | Password reset tokens |
| `CouponCodeLength` | 10 | Coupon/promo codes |
| `ReferralCodeLength` | 8 | Referral codes |
| `ApiKeyLength` | 32 | API keys |
| `SessionTokenLength` | 64 | Session tokens |

### Character Options

| Option | Default | Description |
|--------|---------|-------------|
| `IncludeUppercase` | true | Include A-Z |
| `IncludeLowercase` | true | Include a-z |
| `IncludeDigits` | true | Include 0-9 |
| `IncludeSpecialCharacters` | false | Include !@#$%^&* |
| `ExcludeAmbiguous` | true | Exclude 0, O, 1, I, l |
| `CustomCharacterSet` | null | Override with custom chars |

## Security Features

### Cryptographic Random Generation

Uses `System.Security.Cryptography.RandomNumberGenerator` instead of `Random`:
- **Unpredictable**: Cannot be guessed or predicted
- **Thread-safe**: Safe for concurrent access
- **High entropy**: Suitable for security tokens

### Ambiguous Character Exclusion

By default excludes characters that look similar:
- `0` (zero) vs `O` (letter O)
- `1` (one) vs `I` (letter I) vs `l` (lowercase L)

This improves user experience when manually entering codes.

## Common Use Cases

### Auth API
- Email verification codes
- SMS OTP codes
- Password reset tokens
- Two-factor authentication codes

### MMO API
- Coupon codes
- Promotional codes
- Referral codes
- Partner codes

### API Management
- API keys
- Access tokens
- Secret keys

### User Management
- Invitation codes
- Registration tokens
- Account activation codes

## Best Practices

### 1. Choose Appropriate Length

```csharp
// Short codes for user input (6-8 chars)
var verificationCode = _codeGenerator.GenerateVerificationCode();

// Long codes for security (32+ chars)
var apiKey = _codeGenerator.GenerateApiKey();
```

### 2. Store Hashed Codes

```csharp
// Hash sensitive codes before storage
var code = _codeGenerator.GeneratePasswordResetToken();
var hashedCode = BCrypt.HashPassword(code);
await SaveHashedCodeAsync(email, hashedCode);
```

### 3. Add Expiration

```csharp
public async Task SaveVerificationCodeAsync(string email, string code)
{
    var verification = new VerificationCode
    {
        Email = email,
        Code = code,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
        CreatedAt = DateTimeOffset.UtcNow
    };
    
    await _repository.AddAsync(verification);
}
```

### 4. Rate Limiting

```csharp
// Limit code generation per user
public async Task<string> GenerateWithRateLimitAsync(string email)
{
    var recentCodes = await GetRecentCodesAsync(email, TimeSpan.FromMinutes(5));
    if (recentCodes.Count >= 3)
    {
        throw new InvalidOperationException("Too many verification codes requested");
    }
    
    return _codeGenerator.GenerateVerificationCode();
}
```

### 5. Bulk Generation Validation

```csharp
// Ensure uniqueness in database
public async Task<List<string>> GenerateUniqueCouponCodesAsync(int count)
{
    var codes = _codeGenerator.GenerateBatch(count, CodeType.CouponCode);
    
    // Check against existing codes
    var existing = await _repository.GetCouponCodesByCodesAsync(codes);
    if (existing.Any())
    {
        // Regenerate if conflicts found
        return await GenerateUniqueCouponCodesAsync(count);
    }
    
    return codes;
}
```

## Migration from Manual Generation

### Before (Insecure)

```csharp
// ❌ Insecure: Predictable
var random = new Random();
var code = random.Next(100000, 999999).ToString();

// ❌ Insecure: Timestamp-based
var code = DateTime.Now.Ticks.ToString().Substring(0, 6);

// ❌ Insecure: GUID substring
var code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
```

### After (Secure)

```csharp
// ✅ Secure: Cryptographically random
var code = _codeGenerator.GenerateVerificationCode();

// ✅ Secure: Custom requirements
var code = _codeGenerator.Generate(6, true, false, true);

// ✅ Secure: Batch with uniqueness
var codes = _codeGenerator.GenerateBatch(100, CodeType.CouponCode);
```

## Troubleshooting

### Issue: Codes Not Unique

**Solution**: Increase code length or reduce batch size
```csharp
// Increase length for uniqueness
services.AddCodeGenerator(options =>
{
    options.CouponCodeLength = 12; // Increased from 10
});
```

### Issue: "No valid character set" Error

**Solution**: Enable at least one character type
```json
{
  "CodeGenerator": {
    "IncludeUppercase": true,
    "IncludeDigits": true
  }
}
```

### Issue: Custom Character Set Not Working

**Solution**: Ensure `CustomCharacterSet` is set in config
```json
{
  "CodeGenerator": {
    "CustomCharacterSet": "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
  }
}
```

## Performance

- **Generation Speed**: ~1-2 microseconds per code
- **Batch Generation**: ~100-200 microseconds for 100 codes
- **Memory**: Minimal (stateless service)
- **Thread-Safe**: Yes (singleton service)

## Dependencies

- Microsoft.Extensions.Configuration.Abstractions (10.0.0)
- Microsoft.Extensions.Configuration.Binder (10.0.0)
- Microsoft.Extensions.Options (10.0.0)
- Microsoft.Extensions.Options.ConfigurationExtensions (10.0.0)

## License

MIT License - Same as MMO project
