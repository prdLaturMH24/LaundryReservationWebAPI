using Microsoft.AspNetCore.Mvc;

namespace ReservationWebAPI.Helpers
{
    public class SerilogConfiguration
    {
        public SerilogConfiguration() { }
        public MinimumLevel MinimumLevel { get; set; }
        public IEnumerable<WriteTo> WriteTo { get; set; }
        public IEnumerable<string> Enrich {  get; set; }

        
    }

    public class MinimumLevel
    {
        public required string Default { get; set; }
        public required object Override { get; set; }
    }


    public class WriteTo
    {
        public string Name { get; set; } = string.Empty;
        public object Args { get; set; }
    }
}
