namespace AIHackathon.DB.Models
{
    public class SharedRegisterModelBase
    {
        public string? Surname { get; set; }
#if DEBUGTEST
        = "Иванов";
#endif
        public string? Email { get; set; }
#if DEBUGTEST
        = "test@test.com";
#endif
        public string? Phone { get; set; }
#if DEBUGTEST
        = "+77777777777";
#endif
    }
    public class SharedRegisterModel : SharedRegisterModelBase
    {
        public string? Value { get; set; }
    }
}
