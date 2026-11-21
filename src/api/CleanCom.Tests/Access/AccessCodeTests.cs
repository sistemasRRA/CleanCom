using CleanCom.Domain.Access;
using CleanCom.Domain.Common;

namespace CleanCom.Tests.Access;

/// <summary>
/// Sign-in code of the invitado app (docs/pantallas/apps/invitado.md, Registro y acceso): valid 10 minutes, 5 attempts,
/// a new one after 60 seconds and up to 5 an hour (Acceso.* settings).
/// </summary>
public class AccessCodeTests
{
    private const string Phone = "5512345678";
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 18, 0, 0, TimeSpan.Zero);

    private static AccessCode Code(string code, DateTimeOffset? expires = null) =>
        new() { Phone = Phone, CodeHash = AccessCodes.Hash(Phone, code), ExpiresAt = expires ?? Now.AddMinutes(10), CreatedAt = Now };

    [Fact]
    public void A_code_has_six_digits_and_only_its_hash_is_kept()
    {
        var code = AccessCodes.Generate();
        Assert.Matches("^[0-9]{6}$", code);
        var stored = Code(code);
        Assert.DoesNotContain(code, stored.CodeHash);
        // The same code for another phone is another hash.
        Assert.NotEqual(AccessCodes.Hash(Phone, "123456"), AccessCodes.Hash("5587654321", "123456"));
    }

    [Fact]
    public void The_right_code_opens_once()
    {
        var stored = Code("123456");
        AccessCodes.Verify(stored, Phone, " 123456 ", Now, 5);
        Assert.NotNull(stored.ClosedAt);
        Assert.Throws<DomainException>(() => AccessCodes.Verify(stored, Phone, "123456", Now, 5));
    }

    [Fact]
    public void A_wrong_code_says_how_many_attempts_are_left_and_closes_on_the_last_one()
    {
        var stored = Code("123456");
        var first = Assert.Throws<DomainException>(() => AccessCodes.Verify(stored, Phone, "000000", Now, 5));
        Assert.Equal("El código no es correcto. Te quedan 4 intentos.", first.Message);
        for (var i = 0; i < 3; i++)
        {
            Assert.Throws<DomainException>(() => AccessCodes.Verify(stored, Phone, "000000", Now, 5));
        }
        var last = Assert.Throws<DomainException>(() => AccessCodes.Verify(stored, Phone, "000000", Now, 5));
        Assert.Contains("ya no quedan intentos", last.Message);
        // Not even the right code works after that.
        Assert.Throws<DomainException>(() => AccessCodes.Verify(stored, Phone, "123456", Now, 5));
    }

    [Fact]
    public void An_expired_code_does_not_open()
    {
        var stored = Code("123456", expires: Now.AddSeconds(-1));
        var error = Assert.Throws<DomainException>(() => AccessCodes.Verify(stored, Phone, "123456", Now, 5));
        Assert.Equal("El código venció. Pide uno nuevo.", error.Message);
    }

    [Fact]
    public void A_new_code_waits_60_seconds_and_at_most_5_go_in_an_hour()
    {
        AccessCodes.EnsureCanSend([], Now, 60, 5);
        var wait = Assert.Throws<DomainException>(() => AccessCodes.EnsureCanSend([Now.AddSeconds(-20)], Now, 60, 5));
        Assert.Equal("Espera 40 segundos para pedir otro código.", wait.Message);
        AccessCodes.EnsureCanSend([Now.AddSeconds(-61)], Now, 60, 5);

        var five = Enumerable.Range(1, 5).Select(i => Now.AddMinutes(-10 * i)).ToList();
        Assert.Throws<DomainException>(() => AccessCodes.EnsureCanSend(five, Now, 60, 5));
    }

    [Fact]
    public void The_phone_is_shown_half_hidden()
    {
        Assert.Equal("55 **** 5678", AccessCodes.Mask(Phone));
    }
}
