import { AIFeaturesSection } from "@/components/landing/ai-features-section";
import { CategorySection } from "@/components/landing/category-section";
import { FeaturedProjectsSection } from "@/components/landing/featured-projects-section";
import { FinalCTASection } from "@/components/landing/final-cta-section";
import { HeroSection } from "@/components/landing/hero-section";
import { HowItWorksSection } from "@/components/landing/how-it-works-section";
import { LandingFooter } from "@/components/landing/landing-footer";
import { LandingHeader } from "@/components/landing/landing-header";
import { ProjectSearchBar } from "@/components/landing/project-search-bar";
import { TrustSafetySection } from "@/components/landing/trust-safety-section";
import { UserRoleSection } from "@/components/landing/user-role-section";

export default function HomePage() {
  return (
    <main className="min-h-screen bg-background">
      <LandingHeader />
      <HeroSection />
      <ProjectSearchBar />
      <FeaturedProjectsSection />
      <CategorySection />
      <HowItWorksSection />
      <UserRoleSection />
      <AIFeaturesSection />
      <TrustSafetySection />
      <FinalCTASection />
      <LandingFooter />
    </main>
  );
}
