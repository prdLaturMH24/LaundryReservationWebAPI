namespace ReservationWebAPI
{
    public class AppSettings
    {
        public string BaseAddress { get; private set; }
        public string LaundryDbConnectionString { get; private set; }

        public AppSettings(
            string baseAddress,
            string laundryDbConnectionString
            ) 
        {
            BaseAddress = baseAddress;
            LaundryDbConnectionString = laundryDbConnectionString;
        }
    }
}
