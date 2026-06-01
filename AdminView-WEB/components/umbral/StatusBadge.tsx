import { MissionStatus, OperatorStatus } from "@/lib/types";
import { cn } from "@/lib/utils";

type Status = MissionStatus | OperatorStatus;

const statusConfig: Record<Status, { label: string; className: string }> = {
  Draft: {
    label: "Draft",
    className: "bg-zinc-100 text-zinc-600 border border-zinc-200",
  },
  Active: {
    label: "Active",
    className: "bg-emerald-50 text-emerald-700 border border-emerald-200",
  },
  Inactive: {
    label: "Inactive",
    className: "bg-red-50 text-red-600 border border-red-200",
  },
};

interface StatusBadgeProps {
  status: Status;
  className?: string;
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = statusConfig[status];
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium",
        config.className,
        className
      )}
    >
      <span
        className={cn("h-1.5 w-1.5 rounded-full", {
          "bg-zinc-500": status === "Draft",
          "bg-emerald-500": status === "Active",
          "bg-red-500": status === "Inactive",
        })}
      />
      {config.label}
    </span>
  );
}
