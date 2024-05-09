using Microsoft.EntityFrameworkCore.Design;

namespace EmptyProject {
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext> {
        public DataContext CreateDbContext(string[] args) {
            var context = new DataContext();
            context.InitializeData();
            return context;
        }
    }
}