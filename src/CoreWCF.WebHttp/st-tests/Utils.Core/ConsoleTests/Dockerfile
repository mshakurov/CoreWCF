FROM mcr.microsoft.com/dotnet/runtime:6.0-focal AS runtime
RUN sed -i 's/TLSv1.2/TLSv1.0/g' /etc/ssl/openssl.cnf
WORKDIR /app
COPY bin/release/net6.0/linux-x64/publish/*.* ./
ENTRYPOINT ["dotnet", "ConsoleTests.dll"]