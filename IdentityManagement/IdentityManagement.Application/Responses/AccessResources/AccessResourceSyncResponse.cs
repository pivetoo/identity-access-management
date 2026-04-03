namespace IdentityManagement.Application.Responses.AccessResources
{
    public class AccessResourceSyncResponse
    {
        public int CreatedCount { get; set; }

        public int UpdatedCount { get; set; }

        public int DeactivatedCount { get; set; }

        public int TotalCount { get; set; }
    }
}
