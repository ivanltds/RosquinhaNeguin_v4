using System.Text;

namespace RosquinhaNeguin.Helpers;

public static class PixGenerator
{
    public static string GeneratePayload(string pixKey, string beneficiaryName, string city, decimal amount, string txid = "***")
    {
        var payload = new StringBuilder();

        // 00: Payload Format Indicator (Fixo: "0201")
        payload.Append("000201");

        // 26: Merchant Account Information
        // Sub-campos:
        //   00: GUI (Fixo: "br.gov.pix")
        //   01: Chave Pix
        string gui = "0014br.gov.pix";
        string key = $"01{pixKey.Length:D2}{pixKey}";
        string merchantAccountInfo = gui + key;
        payload.Append($"26{merchantAccountInfo.Length:D2}{merchantAccountInfo}");

        // 52: Merchant Category Code (Fixo: "0000")
        payload.Append("52040000");

        // 53: Transaction Currency (BRL ISO 4217: "986")
        payload.Append("5303986");

        // 54: Transaction Amount
        string amountStr = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        payload.Append($"54{amountStr.Length:D2}{amountStr}");

        // 58: Country Code (Fixo: "BR")
        payload.Append("5802BR");

        // 59: Merchant Name (Max 25 chars)
        string nameClean = CleanString(beneficiaryName);
        if (nameClean.Length > 25) nameClean = nameClean.Substring(0, 25);
        payload.Append($"59{nameClean.Length:D2}{nameClean}");

        // 60: Merchant City (Max 15 chars)
        string cityClean = CleanString(city);
        if (cityClean.Length > 15) cityClean = cityClean.Substring(0, 15);
        payload.Append($"60{cityClean.Length:D2}{cityClean}");

        // 62: Additional Data Field Template (Contém txid)
        // Sub-campos:
        //   05: TXID (Fixo ou dinâmico)
        string txidField = $"05{txid.Length:D2}{txid}";
        payload.Append($"62{txidField.Length:D2}{txidField}");

        // 63: CRC16 (Calculado sobre toda a string gerada até aqui, finalizando com "6304")
        payload.Append("6304");

        string rawPayload = payload.ToString();
        string crc = CalculateCRC16(rawPayload);

        return rawPayload + crc;
    }

    private static string CleanString(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // Remove acentos e caracteres especiais para compatibilidade com o formato EMV
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                // Aceita apenas letras, números e espaços
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ')
                {
                    sb.Append(c);
                }
            }
        }
        return sb.ToString();
    }

    private static string CalculateCRC16(string payload)
    {
        ushort polynomial = 0x1021;
        ushort crc = 0xFFFF;
        byte[] bytes = Encoding.UTF8.GetBytes(payload);

        foreach (byte b in bytes)
        {
            for (int i = 0; i < 8; i++)
            {
                bool bit = ((b >> (7 - i)) & 1) == 1;
                bool c15 = ((crc >> 15) & 1) == 1;
                crc <<= 1;
                if (c15 ^ bit) crc ^= polynomial;
            }
        }
        return (crc & 0xFFFF).ToString("X4");
    }
}
