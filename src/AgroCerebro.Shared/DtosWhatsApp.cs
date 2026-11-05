namespace AgroCerebro.Shared;

public record InstanciaWaDto(string Id, string? Nome, string? Status, string? Provider, bool Ativa);
public record WaAdminStatusDto(bool ApiKeyConfigurada, string? InstanciaAtiva, string? Erro, List<InstanciaWaDto> Instancias);
public record CriarInstanciaWaRequest(string Nome, string? Provider);
public record QrWaDto(string? Qr, string? Status, string? Bruto);
