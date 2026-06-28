import { AIFeaturesSection } from "@/components/landing/ai-features-section";
import { CategorySection } from "@/components/landing/category-section";
import { FeaturedProjectsSection } from "@/components/landing/featured-projects-section";
import { FinalCTASection } from "@/components/landing/final-cta-section";
import { HeroSection } from "@/components/landing/hero-section";
import { HowItWorksSection } from "@/components/landing/how-it-works-section";

import { ProjectSearchBar } from "@/components/landing/project-search-bar";
import { TrustSafetySection } from "@/components/landing/trust-safety-section";
import { UserRoleSection } from "@/components/landing/user-role-section";

export default function HomePage() {
  return (
    <main className="bg-background">
      <HeroSection />
      <ProjectSearchBar />
      <FeaturedProjectsSection />
      <CategorySection />
      <HowItWorksSection />
      <UserRoleSection />
      <AIFeaturesSection />
      <TrustSafetySection />
      <FinalCTASection />
    </main>
  );
}
