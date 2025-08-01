# ####################################################################################
# # Dockerfile Multi-Stage para Aplicação .NET (Versão Robusta)                      #
# # -------------------------------------------------------------------------------- #
# # Este arquivo é a "receita" final que ensina o Docker a construir a imagem        #
# # para nossa aplicação. A abordagem "multi-stage" garante um contêiner final      #
# # pequeno, seguro e otimizado.                                                     #
# ####################################################################################


# ####################################################################################
# # Estágio 1: Build (A Oficina de Montagem)                                         #
# ####################################################################################
# 'FROM' define a imagem base. Começamos com a imagem oficial do .NET SDK 8.0,
# que contém todas as ferramentas necessárias para compilar nosso código.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# 'WORKDIR' cria um diretório de trabalho chamado '/app' dentro do contêiner.
# É a nossa "mesa de trabalho" virtual.
WORKDIR /app

# 'COPY' copia arquivos da sua máquina para dentro do contêiner.
# Para otimizar o cache do Docker, primeiro copiamos apenas os arquivos que
# definem a estrutura e as dependências do projeto.
COPY OrionHealth.sln .
COPY src/OrionHealth.Application/OrionHealth.Application.csproj src/OrionHealth.Application/
COPY src/OrionHealth.CrossCutting/OrionHealth.CrossCutting.csproj src/OrionHealth.CrossCutting/
COPY src/OrionHealth.Domain/OrionHealth.Domain.csproj src/OrionHealth.Domain/
COPY src/OrionHealth.Infrastructure/OrionHealth.Infrastructure.csproj src/OrionHealth.Infrastructure/
COPY src/OrionHealth.Worker/OrionHealth.Worker.csproj src/OrionHealth.Worker/

# 'RUN dotnet restore' lê o arquivo de solução (.sln) e baixa todos os pacotes NuGet
# necessários para todos os projetos. Como fazemos isso antes de copiar o resto do código,
# o Docker guardará essa camada em cache, acelerando builds futuros.
RUN dotnet restore

# Agora que as dependências foram restauradas, copiamos todo o resto do código fonte
# (os arquivos .cs) para a estrutura de pastas correspondente dentro do contêiner.
COPY . .

# 'RUN dotnet publish' é o comando que compila nosso código e o prepara para produção.
# '-c Release' garante que ele seja otimizado.
# '-o out' coloca o resultado final e limpo em uma pasta chamada 'out'.
RUN dotnet publish -c Release -o out src/OrionHealth.Worker/OrionHealth.Worker.csproj


# ####################################################################################
# # Estágio 2: Final (O Produto Final, Pronto para Envio)                            #
# ####################################################################################
# Começamos de novo com uma imagem base muito menor, a 'runtime'.
# Ela tem apenas o necessário para EXECUTAR a aplicação, tornando nossa imagem final leve.
FROM mcr.microsoft.com/dotnet/runtime:9.0

# Definimos o mesmo diretório de trabalho na nossa nova imagem limpa.
WORKDIR /app

# 'COPY --from=build' é a mágica do multi-stage. Copiamos APENAS a pasta 'out'
# (o resultado da compilação) do estágio 'build' para a nossa imagem final.
COPY --from=build /app/out .

# 'ENTRYPOINT' define o comando que será executado para iniciar nossa aplicação
# assim que o contêiner for iniciado.
ENTRYPOINT ["dotnet", "OrionHealth.Worker.dll"]