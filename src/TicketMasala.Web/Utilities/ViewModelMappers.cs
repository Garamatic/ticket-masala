namespace TicketMasala.Web.Utilities;
    public static class ViewModelMappers
    {
        public static int ComputeProgressPercent(int done, int total)
        {
            if (total == 0) return 0;
            return (int)Math.Round((done / (double)total) * 100);
        }
}
