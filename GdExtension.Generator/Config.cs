namespace GdExtension;

public record Config(OnReadyConfig? OnReady, List<ResourceConfig>? Resources);

public record OnReadyConfig;

public record ResourceConfig(string Path);
