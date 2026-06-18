"use client";

import { useEffect, useState } from "react";
import { cn } from "@/lib/utils";
import {
  MapIcon,
  UsersIcon,
  ShieldAlertIcon,
  LogOutIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  getAuthSession,
  getSessionRoleLabel,
  getSessionUsername,
  redirectToLogin,
  type AuthSession,
} from "@/lib/auth/session";

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

function getInitials(label: string): string {
  return label
    .split(/[\s@.]+/)
    .filter(Boolean)
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

export function Sidebar({ active, onNavigate }: SidebarProps) {
  const [session, setSession] = useState<AuthSession | null>(null);

  useEffect(() => {
    setSession(getAuthSession());
  }, []);

  const roleLabel = session ? getSessionRoleLabel(session) : "Administrator";
  const username = session ? getSessionUsername(session) : "";
  const initials = getInitials(username || roleLabel);

  return (
    <aside className="flex flex-col w-56 shrink-0 h-screen bg-sidebar border-r border-sidebar-border sticky top-0">
      <div className="flex items-center gap-2.5 px-5 py-5 border-b border-sidebar-border">
        <div className="h-7 w-7 rounded-md bg-sidebar-primary flex items-center justify-center shrink-0">
          <ShieldAlertIcon className="h-4 w-4 text-sidebar-primary-foreground" />
        </div>
        <div>
          <p className="text-sm font-bold text-sidebar-primary leading-none tracking-wide">UMBRAL</p>
          <p className="text-xs text-sidebar-foreground/50 mt-0.5">Admin Console</p>
        </div>
      </div>

      <div className="px-5 pt-5 pb-2">
        <p className="text-xs font-medium text-sidebar-foreground/40 uppercase tracking-wider">Navigation</p>
      </div>

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

      <div className="px-5 py-4 border-t border-sidebar-border space-y-3">
        <div className="flex items-center gap-2.5">
          <div className="h-7 w-7 rounded-full bg-sidebar-accent flex items-center justify-center shrink-0">
            <span className="text-xs font-semibold text-sidebar-accent-foreground">{initials}</span>
          </div>
          <div className="min-w-0">
            <p className="text-xs font-medium text-sidebar-foreground leading-none">{roleLabel}</p>
            {username && (
              <p className="text-xs text-sidebar-foreground/40 truncate mt-0.5">{username}</p>
            )}
          </div>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="w-full justify-start gap-2 text-sidebar-foreground/70 hover:text-sidebar-foreground"
          onClick={() => redirectToLogin()}
        >
          <LogOutIcon className="h-4 w-4" />
          Sign out
        </Button>
      </div>
    </aside>
  );
}
