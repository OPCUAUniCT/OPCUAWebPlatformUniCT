FROM microsoft/aspnetcore
WORKDIR /app
COPY . .
CMD dotnet OPCUA_Web_Platform.dll
EXPOSE 5000