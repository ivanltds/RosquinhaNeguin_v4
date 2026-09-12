using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using RosquinhaNeguin.Models;

namespace RosquinhaNeguin.Helpers;

public static class FreteCalculator
{
    // Origem da Loja: CEP 06250-250 (Jd. Elvira, Osasco - SP)
    public const string CepOrigem = "06250250";
    public const string CidadeOrigem = "Osasco";
    public const string UfOrigem = "SP";
    public const double LatOrigem = -23.5186;
    public const double LonOrigem = -46.8042;

    public const double DistanciaMaximaOutrosMunicipiosKm = 10.0;
    public const decimal ValorBaseFrete = 5.00m; // Até 3km
    public const decimal ValorAdicionalPorKm = 1.50m; // Por km acima de 3km

    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static async Task<ResultadoFreteDto> CalcularFreteAsync(string cepDestinoRaw)
    {
        var cep = new string(cepDestinoRaw.Where(char.IsDigit).ToArray());
        if (cep.Length != 8)
        {
            return new ResultadoFreteDto(false, "CEP inválido. Informe um CEP com 8 dígitos.", 0, 0, "", "", "", "");
        }

        // 1. Buscar informações de endereço via ViaCEP
        ViaCepResponse? viaCepData = null;
        try
        {
            var response = await _httpClient.GetAsync($"https://viacep.com.br/ws/{cep}/json/");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                viaCepData = JsonSerializer.Deserialize<ViaCepResponse>(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FreteCalculator] Erro ViaCEP: {ex.Message}");
        }

        if (viaCepData == null || viaCepData.Erro)
        {
            return new ResultadoFreteDto(false, "CEP não encontrado.", 0, 0, "", "", "", "");
        }

        var cidadeDestino = viaCepData.Localidade ?? "";
        var ufDestino = viaCepData.Uf ?? "";
        var bairroDestino = viaCepData.Bairro ?? "";
        var logradouroDestino = viaCepData.Logradouro ?? "";

        // Se for de outro estado que não seja SP, está fora do raio
        if (!ufDestino.Equals("SP", StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoFreteDto(
                false, 
                $"Não realizamos entregas fora do estado de SP (Localidade: {cidadeDestino}/{ufDestino}).", 
                0, 
                999, 
                logradouroDestino, 
                bairroDestino, 
                cidadeDestino, 
                ufDestino
            );
        }

        // 2. Calcular distância geográfica
        double distanciaKm = await ObterDistanciaKmAsync(cep, viaCepData);

        bool ehMesmoMunicipio = cidadeDestino.Trim().Equals(CidadeOrigem, StringComparison.OrdinalIgnoreCase);

        // Regra de Negócio: Não atende outros municípios se a distância for maior que 10 KM. Se for <= 10 KM, atende!
        if (!ehMesmoMunicipio && distanciaKm > DistanciaMaximaOutrosMunicipiosKm)
        {
            return new ResultadoFreteDto(
                false,
                $"Entrega indisponível para {cidadeDestino}. Não atendemos outros municípios com distância superior a 10 km (Distância calculada: {distanciaKm:F1} km).",
                0,
                distanciaKm,
                logradouroDestino,
                bairroDestino,
                cidadeDestino,
                ufDestino
            );
        }

        if (ehMesmoMunicipio && distanciaKm > 15.0)
        {
            return new ResultadoFreteDto(
                false,
                $"Endereço fora do raio de atendimento de Osasco (Distância: {distanciaKm:F1} km).",
                0,
                distanciaKm,
                logradouroDestino,
                bairroDestino,
                cidadeDestino,
                ufDestino
            );
        }

        // 3. Cálculo do valor do frete progressivo por km
        decimal valorFrete;
        if (distanciaKm <= 3.0)
        {
            valorFrete = ValorBaseFrete; // R$ 5,00
        }
        else
        {
            var kmExcedente = (decimal)(distanciaKm - 3.0);
            valorFrete = ValorBaseFrete + Math.Round(kmExcedente * ValorAdicionalPorKm, 2);
        }

        string msgSucesso = ehMesmoMunicipio
            ? $"Entrega em {cidadeDestino} ({distanciaKm:F1} km) - Taxa de entrega calculada."
            : $"Entrega intermunicipal ({cidadeDestino}, {distanciaKm:F1} km <= 10 km) - Taxa de entrega calculada.";

        return new ResultadoFreteDto(
            true,
            msgSucesso,
            valorFrete,
            Math.Round(distanciaKm, 1),
            logradouroDestino,
            bairroDestino,
            cidadeDestino,
            ufDestino
        );
    }

    private static async Task<double> ObterDistanciaKmAsync(string cepDestino, ViaCepResponse viaCep)
    {
        var cidade = (viaCep.Localidade ?? "").Trim().ToLower();
        var bairro = (viaCep.Bairro ?? "").Trim().ToLower();

        // 1. Tratamento específico para bairros limítrofes conhecidos da Grande SP e Zona Oeste
        if (cidade == "osasco")
        {
            if (bairro.Contains("elvira") || bairro.Contains("rochdale") || bairro.Contains("piratininga") || bairro.Contains("helenamaria"))
                return 1.5;
            if (bairro.Contains("centro") || bairro.Contains("vila yara") || bairro.Contains("campesina") || bairro.Contains("bela vista"))
                return 4.2;
            return 4.8;
        }

        if (cidade == "carapicuíba" || cidade == "carapicuiba")
        {
            return 5.2; // <= 10km (atende!)
        }

        if (cidade == "barueri")
        {
            if (bairro.Contains("alphaville") || bairro.Contains("tamboré") || bairro.Contains("tambore"))
                return 7.5; // <= 10km (atende!)
            return 8.5; // <= 10km (atende!)
        }

        if (cidade == "são paulo" || cidade == "sao paulo")
        {
            // Bairros limítrofes de São Paulo colados em Osasco (<= 10km)
            if (bairro.Contains("jaguaré") || bairro.Contains("jaguare") || 
                bairro.Contains("leopoldina") || 
                bairro.Contains("vila dos remédios") || bairro.Contains("vila dos remedios") || 
                bairro.Contains("rio pequeno") || 
                bairro.Contains("butantã") || bairro.Contains("butanta") ||
                bairro.Contains("pirituba") || 
                bairro.Contains("jaraguá") || bairro.Contains("jaragua") ||
                bairro.Contains("lapa") || 
                bairro.Contains("pinheiros"))
            {
                return 7.5; // <= 10km (atende!)
            }
        }

        if (cidade == "jandira") return 12.0; // > 10km
        if (cidade == "itapevi") return 15.0; // > 10km
        if (cidade == "santana de parnaíba" || cidade == "santana de parnaiba") return 13.5; // > 10km
        if (cidade == "cotia") return 16.0; // > 10km
        if (cidade == "taboão da serra" || cidade == "taboao da serra") return 12.5; // > 10km
        if (cidade == "embú das artes" || cidade == "embu das artes") return 18.0; // > 10km
        if (cidade == "guarulhos") return 32.0; // > 10km
        if (cidade == "santo andré" || cidade == "santo andre" || cidade == "são bernardo do campo" || cidade == "sao bernardo do campo") return 35.0; // > 10km

        // 2. Tentar obter coordenadas exatas via BrasilAPI v2 para outros CEPs
        try
        {
            var url = $"https://brasilapi.com.br/api/cep/v2/{cepDestino}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("location", out var locationElem) &&
                    locationElem.TryGetProperty("coordinates", out var coordsElem))
                {
                    if (coordsElem.TryGetProperty("latitude", out var latElem) &&
                        coordsElem.TryGetProperty("longitude", out var lonElem))
                    {
                        if (double.TryParse(latElem.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double destLat) &&
                            double.TryParse(lonElem.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double destLon))
                        {
                            if (destLat != 0 && destLon != 0)
                            {
                                double dReta = CalcularDistanciaHaversine(LatOrigem, LonOrigem, destLat, destLon);
                                return Math.Round(dReta * 1.25, 1); // Fator de rota urbana de ruas
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fallback
        }

        if (cidade == "são paulo" || cidade == "sao paulo")
        {
            return 18.5; // Outras regiões de SP mais distantes (> 10km)
        }

        return 30.0; // Outras cidades distantes (> 10km)
    }

    private static double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Raio da Terra em km
        var dLat = GrausParaRadianos(lat2 - lat1);
        var dLon = GrausParaRadianos(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(GrausParaRadianos(lat1)) * Math.Cos(GrausParaRadianos(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double GrausParaRadianos(double graus) => graus * (Math.PI / 180.0);

    private class ViaCepResponse
    {
        [JsonPropertyName("cep")] public string? Cep { get; set; }
        [JsonPropertyName("logradouro")] public string? Logradouro { get; set; }
        [JsonPropertyName("complemento")] public string? Complemento { get; set; }
        [JsonPropertyName("bairro")] public string? Bairro { get; set; }
        [JsonPropertyName("localidade")] public string? Localidade { get; set; }
        [JsonPropertyName("uf")] public string? Uf { get; set; }
        [JsonPropertyName("erro")] public JsonElement? ErroElement { get; set; }

        [JsonIgnore]
        public bool Erro
        {
            get
            {
                if (!ErroElement.HasValue) return false;
                if (ErroElement.Value.ValueKind == JsonValueKind.True) return true;
                if (ErroElement.Value.ValueKind == JsonValueKind.String && bool.TryParse(ErroElement.Value.GetString(), out bool b)) return b;
                if (ErroElement.Value.ValueKind == JsonValueKind.String && ErroElement.Value.GetString() == "true") return true;
                return false;
            }
        }
    }
}
