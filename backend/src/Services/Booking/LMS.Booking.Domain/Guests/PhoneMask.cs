namespace LMS.Booking.Domain.Guests;

// 电话掩码规则：保留国家码段与本地号首位，中间以 *** 代替，保留末 4 位（对齐原型 "+853 6***4829"）。
// 过短的号码整体掩码，不泄露任何位数。
public static class PhoneMask
{
    private const int MinLength = 8;
    private const int TailLength = 4;

    public static string? Mask(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmed = phone.Trim();
        if (trimmed.Length < MinLength)
        {
            return "***";
        }

        var headLength = Math.Min(trimmed.Length - TailLength - 1, HeadLength(trimmed));

        return $"{trimmed[..headLength]}***{trimmed[^TailLength..]}";
    }

    // 国家码段 + 本地号首位：取到第一个空格后的第一个数字为止；无空格时退化为前 5 位。
    private static int HeadLength(string phone)
    {
        var separator = phone.IndexOf(' ');
        if (separator < 0 || separator + 1 >= phone.Length || separator > 6)
        {
            return 5;
        }

        return separator + 2;
    }
}
