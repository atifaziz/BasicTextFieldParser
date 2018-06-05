# BasicTextFieldParser

BasicTextFieldParser is Visual Basic's [`TextFieldParser`][TextFieldParser] for
.NET Standard, based on Mono's implementation. The API is compatible with the
original `TextFieldParser`.


## Usage


```c#
var csv
    // Remission Times for Acute Myelogenous Leukaemia
    // https://vincentarelbundock.github.io/Rdatasets/doc/boot/aml.html
    // https://vincentarelbundock.github.io/Rdatasets/csv/boot/aml.csv
    = "\"\",\"time\",\"cens\",\"group\"\n"
    + "\"1\",9,1,1\n"
    + "\"2\",13,1,1\n"
    + "\"3\",13,0,1\n"
    + "\"4\",18,1,1\n"
    + "\"5\",23,1,1\n"
    + "\"6\",28,0,1\n"
    + "\"7\",31,1,1\n"
    + "\"8\",34,1,1\n"
    + "\"9\",45,0,1\n"
    + "\"10\",48,1,1\n"
    + "\"11\",161,0,1\n"
    + "\"12\",5,1,2\n"
    + "\"13\",5,1,2\n"
    + "\"14\",8,1,2\n"
    + "\"15\",8,1,2\n"
    + "\"16\",12,1,2\n"
    + "\"17\",16,0,2\n"
    + "\"18\",23,1,2\n"
    + "\"19\",27,1,2\n"
    + "\"20\",30,1,2\n"
    + "\"21\",33,1,2\n"
    + "\"22\",43,1,2\n"
    + "\"23\",45,1,2";

using (var parser = new TextFieldParser(new StringReader(csv))
{
    TextFieldType             = FieldType.Delimited,
    Delimiters                = new[] { "," },
    HasFieldsEnclosedInQuotes = true
})
{
    while (!parser.EndOfData)
    {
        var fields = parser.ReadFields();
        var row = new
        {
            Time  = fields[1],
            Cens  = fields[2],
            Group = fields[3],
        };
        Console.WriteLine(row);
    }
}
```


## Building

To build the project, install .NET Core SDK 2.1.200 and run `dotnet build`.


[TextFieldParser]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.textfieldparser
