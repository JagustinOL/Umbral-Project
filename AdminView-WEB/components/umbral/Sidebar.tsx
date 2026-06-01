"use client";

import { cn } from "@/lib/utils";
import {
  MapIcon,
  UsersIcon,
  LayoutDashboardIcon,
  ShieldAlertIcon,
} from "lucide-react";

export type NavSection = "catalog" | "operators";

interface SidebarProps {
  active: NavSection;
  onNavigate: (section: NavSection) => void;
}

const NAV_ITEMS: { id: NavSection; label: string; icon: React.ReactNode; description: string }[] = [
  {
    id: "catalog",
    label: "Missions",
    icon: <MapIcon className="h-4 w-4" />,
    description: "Catalog & Builder",
  },
  {
    id: "operators",
    label: "Operators",
    icon: <UsersIcon className="h-4 w-4" />,
    description: "Accounts & Assignments",
  },
];

export function Sidebar({ active, onNavigate }: SidebarProps) {
  return (
    <aside className="flex flex-col w-56 shrink-0 h-screen bg-sidebar border-r border-sidebar-border sticky top-0">
      {/* Logo / Brand */}
      <div className="flex items-center gap-2.5 px-5 py-5 border-b border-sidebar-border">
        <div className="h-7 w-7 rounded-md bg-sidebar-primary flex items-center justify-center shrink-0">
          <ShieldAlertIcon className="h-4 w-4 text-sidebar-primary-foreground" />
        </div>
        <div>
          <p className="text-sm font-bold text-sidebar-primary leading-none tracking-wide">UMBRAL</p>
          <p className="text-xs text-sidebar-foreground/50 mt-0.5">Admin Console</p>
        </div>
      </div>

      {/* Nav label */}
      <div className="px-5 pt-5 pb-2">
        <p className="text-xs font-medium text-sidebar-foreground/40 uppercase tracking-wider">Navigation</p>
      </div>

      {/* Nav items */}
      <nav className="flex-1 px-3 space-y-0.5">
        {NAV_ITEMS.map((item) => (
          <button
            key={item.id}
            onClick={() => onNavigate(item.id)}
            className={cn(
              "w-full flex items-center gap-3 px-3 py-2.5 rounded-md text-left transition-colors",
              active === item.id
                ? "bg-sidebar-accent text-sidebar-accent-foreground"
                : "text-sidebar-foreground/70 hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground"
            )}
          >
            <span className={cn(
              "shrink-0",
              active === item.id ? "text-sidebar-accent-foreground" : "text-sidebar-foreground/50"
            )}>
              {item.icon}
            </span>
            <div className="min-w-0">
              <p className="text-sm font-medium leading-none">{item.label}</p>
              <p className="text-xs text-sidebar-foreground/40 mt-0.5 leading-none">{item.description}</p>
            </div>
            {active === item.id && (
              <span className="ml-auto h-1.5 w-1.5 rounded-full bg-sidebar-primary shrink-0" />
            )}
          </button>
        ))}
      </nav>

      {/* Footer */}
      <div className="px-5 py-4 border-t border-sidebar-border">
        <div className="flex items-center gap-2.5">
          <div className="h-7 w-7 rounded-full bg-sidebar-accent flex items-center justify-center shrink-0">
            <span className="text-xs font-semibold text-sidebar-accent-foreground">AD</span>
          </div>
          <div className="min-w-0">
            <p className="text-xs font-medium text-sidebar-foreground leading-none">Administrator</p>
            <p className="text-xs text-sidebar-foreground/40 truncate mt-0.5">admin@umbral.ops</p>
          </div>
        </div>
      </div>
    </aside>
  );
}
