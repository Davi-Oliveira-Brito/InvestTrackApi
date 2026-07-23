# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro (otimiza cache do Docker)
# Restringe ao projeto Api e suas ProjectReferences (Application, Infrastructure, Domain) —
# não usa a solution inteira para não depender dos projetos de teste, que não são copiados
# para a imagem.
COPY InvestTrack.Api/InvestTrack.Api.csproj InvestTrack.Api/
COPY InvestTrack.Application/InvestTrack.Application.csproj InvestTrack.Application/
COPY InvestTrack.Infrastructure/InvestTrack.Infrastructure.csproj InvestTrack.Infrastructure/
COPY InvestTrack.Domain/InvestTrack.Domain.csproj InvestTrack.Domain/

RUN dotnet restore InvestTrack.Api/InvestTrack.Api.csproj

# Copia o código dos projetos de produção (sem os projetos de teste)
COPY InvestTrack.Api/ InvestTrack.Api/
COPY InvestTrack.Application/ InvestTrack.Application/
COPY InvestTrack.Infrastructure/ InvestTrack.Infrastructure/
COPY InvestTrack.Domain/ InvestTrack.Domain/

# Publica a API
RUN dotnet publish InvestTrack.Api/InvestTrack.Api.csproj -c Release -o /app/publish

# Etapa 2: runtime (imagem final, mais leve)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render define a porta via variável de ambiente PORT
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InvestTrack.Api.dll"]