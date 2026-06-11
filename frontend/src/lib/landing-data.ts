import {
  BadgeCheck,
  Banknote,
  Bot,
  BrainCircuit,
  GraduationCap,
  HeartPulse,
  History,
  Leaf,
  LucideIcon,
  PackageCheck,
  ShieldCheck,
  ShoppingBag,
  Sprout,
  Truck,
  UsersRound,
  Eye,
} from "lucide-react";
import type { LandingProject } from "@/types/project";

export interface LandingCategory {
  name: string;
  field: string;
  description: string;
  icon: LucideIcon;
}

export interface LandingStep {
  title: string;
  description: string;
  icon: LucideIcon;
}

export interface RoleCard {
  id?: string;
  title: string;
  question: string;
  description: string;
  cta: string;
  href: string;
}

export interface SafetyFeature {
  title: string;
  description: string;
  icon: LucideIcon;
}

export const popularKeywords = ["AI", "EdTech", "FinTech", "E-commerce", "GreenTech"];

export const projectFields = [
  "Tất cả lĩnh vực",
  "AI",
  "EdTech",
  "FinTech",
  "E-commerce",
  "GreenTech",
  "HealthTech",
  "Logistics",
];

export const featuredProjects: LandingProject[] = [
  {
    id: "ecotrack",
    title: "EcoTrack",
    summary: "Nền tảng giúp doanh nghiệp theo dõi và giảm lượng phát thải trong vận hành.",
    field: "GreenTech",
    stage: "MVP",
    status: "Đang tuyển",
    roles: ["Backend Developer", "UI/UX", "Marketing"],
    memberCount: 4,
    seekingInvestor: true,
    href: "/projects/ecotrack",
  },
  {
    id: "edumentor-ai",
    title: "EduMentor AI",
    summary: "Trợ lý học tập cá nhân hóa cho học sinh và giáo viên trong lớp học số.",
    field: "EdTech",
    stage: "Prototype",
    status: "Đang tuyển",
    roles: ["AI Engineer", "Frontend Developer"],
    memberCount: 3,
    seekingInvestor: false,
    href: "/projects/edumentor-ai",
  },
  {
    id: "finwise",
    title: "FinWise",
    summary: "Công cụ quản lý tài chính cá nhân dành cho người mới bắt đầu đầu tư.",
    field: "FinTech",
    stage: "Idea",
    status: "Đang tuyển",
    roles: ["Business Analyst", "Backend Developer"],
    memberCount: 2,
    seekingInvestor: false,
    href: "/projects/finwise",
  },
  {
    id: "localfarm",
    title: "LocalFarm",
    summary: "Kết nối nông trại địa phương với cửa hàng và người tiêu dùng trong khu vực.",
    field: "AgriTech",
    stage: "MVP",
    status: "Đang tìm nhà đầu tư",
    roles: ["Mobile Developer", "Marketing"],
    memberCount: 5,
    seekingInvestor: true,
    href: "/projects/localfarm",
  },
  {
    id: "healthlink",
    title: "HealthLink",
    summary: "Nền tảng hỗ trợ đặt lịch tư vấn sức khỏe và theo dõi hồ sơ chăm sóc.",
    field: "HealthTech",
    stage: "Research",
    status: "Đang tuyển",
    roles: ["UI/UX", "Medical Advisor"],
    memberCount: 3,
    seekingInvestor: false,
    href: "/projects/healthlink",
  },
  {
    id: "logiflow",
    title: "LogiFlow",
    summary: "Tối ưu luồng giao nhận cho đội vận hành vừa và nhỏ bằng dữ liệu thời gian thực.",
    field: "Logistics",
    stage: "Beta",
    status: "Đang tìm đối tác",
    roles: ["Backend Developer", "Data Analyst"],
    memberCount: 6,
    seekingInvestor: true,
    href: "/projects/logiflow",
  },
];

export const categories: LandingCategory[] = [
  { name: "Trí tuệ nhân tạo", field: "AI", description: "Sản phẩm dùng AI để tự động hóa và hỗ trợ quyết định.", icon: BrainCircuit },
  { name: "Giáo dục", field: "EdTech", description: "Giải pháp học tập, đào tạo và phát triển kỹ năng.", icon: GraduationCap },
  { name: "Tài chính", field: "FinTech", description: "Thanh toán, quản lý tài chính và công cụ đầu tư.", icon: Banknote },
  { name: "Y tế", field: "HealthTech", description: "Chăm sóc sức khỏe, hồ sơ y tế và tư vấn từ xa.", icon: HeartPulse },
  { name: "Thương mại điện tử", field: "E-commerce", description: "Bán hàng, logistics thương mại và trải nghiệm mua sắm.", icon: ShoppingBag },
  { name: "Môi trường", field: "GreenTech", description: "Giảm phát thải, năng lượng sạch và vận hành bền vững.", icon: Leaf },
  { name: "Nông nghiệp", field: "AgriTech", description: "Chuỗi cung ứng nông sản và công nghệ cho nông trại.", icon: Sprout },
  { name: "Logistics", field: "Logistics", description: "Vận chuyển, kho bãi và tối ưu luồng giao nhận.", icon: Truck },
];

export const howItWorksSteps: LandingStep[] = [
  {
    title: "Tạo hồ sơ dự án",
    description: "Mô tả vấn đề, giải pháp, mục tiêu và nhu cầu tuyển thành viên.",
    icon: PackageCheck,
  },
  {
    title: "Hoàn thiện với AI",
    description: "Nhận gợi ý để trình bày dự án rõ ràng và chuyên nghiệp hơn.",
    icon: Bot,
  },
  {
    title: "Kết nối đúng người",
    description: "Tìm cộng sự, cố vấn, doanh nghiệp và nhà đầu tư phù hợp.",
    icon: UsersRound,
  },
];

export const roleCards: RoleCard[] = [
  {
    id: "founder",
    title: "Founder",
    question: "Bạn có một ý tưởng?",
    description: "Tạo hồ sơ dự án, tuyển thành viên và tìm kiếm cơ hội hợp tác.",
    cta: "Tạo dự án",
    href: "/projects/create",
  },
  {
    title: "Member",
    question: "Bạn muốn tham gia một dự án thực tế?",
    description: "Tìm dự án phù hợp với kỹ năng, sở thích và định hướng của bạn.",
    cta: "Tìm dự án",
    href: "/projects",
  },
  {
    id: "investor",
    title: "Investor",
    question: "Bạn đang tìm kiếm dự án tiềm năng?",
    description: "Khám phá startup giai đoạn đầu và kết nối trực tiếp với Founder.",
    cta: "Khám phá cơ hội đầu tư",
    href: "/projects?seekingInvestment=true",
  },
];

export const aiFeatureList = [
  "Hoàn thiện mô tả dự án",
  "Xác định vấn đề và giải pháp",
  "Gợi ý kỹ năng cần tuyển",
  "Đánh giá mức độ đầy đủ",
  "Hỗ trợ viết lời ứng tuyển",
  "Tóm tắt dự án cho Investor",
];

export const trustFeatures: SafetyFeature[] = [
  {
    title: "Chế độ hiển thị linh hoạt",
    description: "Chọn Public, Limited, Private hoặc Investor Only.",
    icon: Eye,
  },
  {
    title: "Thỏa thuận bảo mật",
    description: "Yêu cầu người dùng đồng ý NDA trước khi xem thông tin chi tiết.",
    icon: ShieldCheck,
  },
  {
    title: "Lịch sử thay đổi",
    description: "Ghi nhận phiên bản và những thay đổi quan trọng của dự án.",
    icon: History,
  },
  {
    title: "Kiểm duyệt nội dung",
    description: "AI hỗ trợ kiểm tra và Moderator xử lý các dự án có dấu hiệu vi phạm.",
    icon: BadgeCheck,
  },
];
