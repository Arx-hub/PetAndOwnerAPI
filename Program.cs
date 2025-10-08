using LemmikkiAPI;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var Lemmikki = new LemmikkiDB();

app.MapGet("/", () => "Hello World!");
app.MapGet("/omistajat", () => Lemmikki.HaeOmistaja());
app.MapGet("/lemmikit", () => Lemmikki.HaeLemmikki());

// mapGet to get the owners phone number with only the name of the pet.
app.MapGet("/lemmikit/{nimi}", (string nimi) =>
{
    string puhelinnumero = Lemmikki.EtsiOmistajanPuhelinnumero(nimi);
    Console.WriteLine($"Haetaan lemmikki nimellä: {nimi}");
    return Results.Ok(puhelinnumero);
});

// mapPost to add a new owner with name and phonenumber as JSON.
app.MapPost("/omistajat", (OmistajaJaLemmikki omistajaJaLemmikki) =>
{
    Lemmikki.LisaaOmistaja(omistajaJaLemmikki.oNimi, omistajaJaLemmikki.puhelinnumero);
    return Lemmikki.HaeOmistaja();
});

// mapPost to add a new pet with name, species and owner's name as JSON.
app.MapPost("/lemmikit", (OmistajaJaLemmikki omistajaJaLemmikki) =>
{
    Lemmikki.LisaaLemmikki(omistajaJaLemmikki.lNimi, omistajaJaLemmikki.laji, omistajaJaLemmikki.oNimi);
    return Lemmikki.HaeLemmikki();
});

// mapPut to update the owners phone number.
app.MapPut("/omistajat/{nimi}", (string nimi, Puhelinpaivitys newNum) =>
{
    Lemmikki.PaivitaPuhelinnumero(nimi, newNum.puhelinnumero);
    return Lemmikki.HaeOmistaja();
});

app.Run();
