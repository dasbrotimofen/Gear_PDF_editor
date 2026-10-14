# Dependency inventory

Pinned NuGet packages in the resolved application:

- Docnet.Core/2.6.0
- Microsoft.Extensions.DependencyInjection/8.0.1
- Microsoft.Extensions.DependencyInjection.Abstractions/8.0.2
- Microsoft.Extensions.Logging/8.0.1
- Microsoft.Extensions.Logging.Abstractions/8.0.2
- Microsoft.Extensions.Options/8.0.2
- Microsoft.Extensions.Primitives/8.0.0
- PDFsharp/6.2.3

Docnet.Core 2.6.0 supplies PDFium native binaries; Windows x64 is included in the launch folder. PDFium notices are copied verbatim from that exact package. PDFsharp 6.2.3 supplies PDF export. No Windows SDK projection DLLs or SDK tools are bundled. The .NET 10 Desktop Runtime is installed separately.
