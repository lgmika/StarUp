using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_projects_search_vector"
                ON projects
                USING GIN (to_tsvector('english',
                    coalesce("Title", '') || ' ' ||
                    coalesce("Summary", '') || ' ' ||
                    coalesce("Problem", '') || ' ' ||
                    coalesce("Solution", '') || ' ' ||
                    coalesce("TargetMarket", '') || ' ' ||
                    coalesce("BusinessModel", '') || ' ' ||
                    coalesce("FundingNeeds", '')));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_user_profiles_search_vector"
                ON user_profiles
                USING GIN (to_tsvector('english',
                    coalesce("Headline", '') || ' ' ||
                    coalesce("Bio", '') || ' ' ||
                    coalesce("Location", '')));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_project_required_roles_search_vector"
                ON project_required_roles
                USING GIN (to_tsvector('english',
                    coalesce("RoleName", '') || ' ' ||
                    coalesce("Description", '')));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_investor_profiles_search_vector"
                ON investor_profiles
                USING GIN (to_tsvector('english',
                    coalesce("DisplayName", '') || ' ' ||
                    coalesce("OrganizationName", '') || ' ' ||
                    coalesce("Bio", '') || ' ' ||
                    coalesce("InvestmentFocus", '')));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_skills_name_trgm"
                ON skills
                USING GIN ("Name" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_users_full_name_trgm"
                ON users
                USING GIN ("FullName" gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_users_full_name_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_skills_name_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_investor_profiles_search_vector";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_project_required_roles_search_vector";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_user_profiles_search_vector";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_projects_search_vector";""");
        }
    }
}
