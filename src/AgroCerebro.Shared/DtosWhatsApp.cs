namespace AgroCerebro.Shared;

/// <summary>Um número de WhatsApp dentro do pool de uma instância (ApiEnvios: /v1/instances/{id}/numbers).</summary>
public record NumeroWaDto(string Id, string? Label, string? Telefone, string? Provider, string? Estado);
public record InstanciaWaDto(string Id, string? Nome, string? Status, string? Provider, bool Ativa, List<NumeroWaDto>? Numeros = null);
public record WaAdminStatusDto(bool ApiKeyConfigurada, string? InstanciaAtiva, string? Erro, List<InstanciaWaDto> Instancias);
public record CriarInstanciaWaRequest(string Nome, string? Provider);
public record AdicionarNumeroWaRequest(string? Label, string? Provider);
public record QrWaDto(string? Qr, string? Status, string? Bruto);
