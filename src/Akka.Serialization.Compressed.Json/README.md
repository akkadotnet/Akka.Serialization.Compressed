# Akka.Serialization.Compressed.Json

This `CompressedJsonSerializer` Akka.NET serializer is a compressed version of the built-in `Akka.Serialization.NewtonSoftJsonSerializer`.

## Configuration

### Using `Akka.Hosting`

You can add this serializer by using the `Akka.Hosting` extension method:

```csharp
using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("compressed", (builder, provider) =>
        {
            builder.WithCompressedJsonSerializer();
        });
    }).Build();

await host.RunAsync();
```

### Using HOCON Configuration

To use this serializer, add these settings to your HOCON configuration:

```HOCON
akka.actor {
  serializers {
    json-gzip = "Akka.Serialization.Compressed.Json.CompressedJsonSerializer, Akka.Serialization.Compressed.Json"
  }
  
  serialization-bindings {
    "Akka.Serialization.Compressed.Json.IShouldCompress, Akka.Serialization.Compressed.Json" = json-gzip
  }
}
```

## Compressing Data Model

### Using The Built-in Marker Interface

The `Akka.Hosting` extension and HOCON configuration above binds the marker `IShouldCompress` interface to the serializer. To start compressing your data model, make your classes inherit from this interface.

```csharp
public sealed class MyDataModel: IShouldCompress
{
    // ...
}
```

### Binding Your Data Model Directly Using `Akka.Hosting`

To bind your your data model to the serializer using `Akka.Hosting`, you can use the extension method below:

```csharp
public static class DataClassOne
{ }

public static class DataClassTwo
{ }
```

```csharp
using var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("compressed", (builder, provider) =>
        {
            builder.WithCompressedJsonSerializer(
                typeof(DataClassOne), 
                typeof(DataClassTwo);
        });
    }).Build();

await host.RunAsync();
```

### Binding Your Data Model Directly Using HOCON Configuration

You can bind your classes to this serializer by registering them in the HOCON configuration:

```HOCON
akka.actor.serialization-bindings {
  "MyAssembly.DataClassOne, MyAssembly" = json-gzip
  "MyAssembly.DataClassTwo, MyAssembly" = json-gzip
}
```