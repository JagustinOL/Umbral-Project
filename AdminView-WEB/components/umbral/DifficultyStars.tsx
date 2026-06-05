import { cn } from "@/lib/utils";

interface DifficultyStarsProps {
  value: number;
  max?: number;
  className?: string;
}

export function DifficultyStars({ value, max = 5, className }: DifficultyStarsProps) {
  return (
    <div className={cn("flex items-center gap-0.5", className)} aria-label={`Difficulty: ${value} out of ${max}`}>
      {Array.from({ length: max }).map((_, i) => (
        <span
          key={i}
          className={cn(
            "h-2 w-2 rounded-sm",
            i < value ? "bg-zinc-700" : "bg-zinc-200"
          )}
        />
      ))}
    </div>
  );
}
