namespace UcpCarPool.Data
{
    // Driver and Passenger were merged into a single "Member" role —
    // every registered user can both post rides (once they add a vehicle)
    // and request rides, just like inDrive/BlaBlaCar. Only Admin stays separate.
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Member = "Member";
    }
}
