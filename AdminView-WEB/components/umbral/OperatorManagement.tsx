"use client";

import { useState } from "react";
import {
  PlusIcon,
  UserXIcon,
  UserCheckIcon,
  ChevronDownIcon,
  ShieldIcon,
  XIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { StatusBadge } from "./StatusBadge";
import { Operator, Mission, CreateOperatorPayload } from "@/lib/types";
import { cn } from "@/lib/utils";

interface OperatorManagementProps {
  operators: Operator[];
  missions: Mission[];
  onOperatorsChange: (operators: Operator[]) => void;
  onMissionsChange: (missions: Mission[]) => void;
}

// ─── Create Operator Modal ─────────────────────────────────────────────────────

interface CreateOperatorModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateOperatorPayload) => void;
}

function CreateOperatorModal({ open, onClose, onSubmit }: CreateOperatorModalProps) {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ firstName, lastName, email });
    setFirstName("");
    setLastName("");
    setEmail("");
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Create Operator Account</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="op-first">First Name <span className="text-destructive">*</span></Label>
              <Input id="op-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="op-last">Last Name <span className="text-destructive">*</span></Label>
              <Input id="op-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
            </div>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="op-email">Email <span className="text-destructive">*</span></Label>
            <Input id="op-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="operator@umbral.ops" required />
          </div>
          <p className="text-xs text-muted-foreground">
            A Keycloak account will be created and the <strong>operator</strong> role assigned automatically.
          </p>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
            <Button type="submit">Create Account</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Assign Operator Sheet ─────────────────────────────────────────────────────

interface AssignOperatorSheetProps {
  open: boolean;
  onClose: () => void;
  missions: Mission[];
  operators: Operator[];
  onAssign: (missionId: string, operatorId: string) => void;
  onRevoke: (missionId: string, operatorId: string) => void;
}

function AssignOperatorSheet({
  open,
  onClose,
  missions,
  operators,
  onAssign,
  onRevoke,
}: AssignOperatorSheetProps) {
  const [selectedMissionId, setSelectedMissionId] = useState<string>("");
  const selectedMission = missions.find((m) => m.id === selectedMissionId);
  const assigned = selectedMission?.assignedOperators ?? [];

  const assignedOperators = assigned
    .map((ref) => operators.find((o) => o.id === ref.operatorId))
    .filter(Boolean) as Operator[];

  const unassignedOperators = operators.filter(
    (o) => !assigned.some((ref) => ref.operatorId === o.id) && o.status === "Active"
  );

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent className="w-[420px] sm:max-w-[420px] overflow-y-auto">
        <SheetHeader>
          <SheetTitle className="text-base font-semibold">Assign Operators to Mission</SheetTitle>
        </SheetHeader>
        <div className="mt-6 space-y-5">
          <div className="space-y-1.5">
            <Label>Select Mission</Label>
            <Select value={selectedMissionId} onValueChange={setSelectedMissionId}>
              <SelectTrigger className="text-sm">
                <SelectValue placeholder="Choose a mission…" />
              </SelectTrigger>
              <SelectContent>
                {missions.map((m) => (
                  <SelectItem key={m.id} value={m.id}>
                    <div className="flex items-center gap-2">
                      <span>{m.title}</span>
                      <span className={cn(
                        "text-xs px-1.5 py-0.5 rounded-full",
                        m.status === "Active" ? "bg-emerald-50 text-emerald-700" :
                        m.status === "Draft" ? "bg-zinc-100 text-zinc-600" :
                        "bg-red-50 text-red-600"
                      )}>{m.status}</span>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {selectedMission && (
            <>
              {/* Assigned operators */}
              <div className="space-y-2">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Assigned ({assignedOperators.length})
                </p>
                {assignedOperators.length === 0 ? (
                  <p className="text-xs text-muted-foreground italic">No operators assigned.</p>
                ) : (
                  <div className="space-y-1.5">
                    {assignedOperators.map((op) => (
                      <div
                        key={op.id}
                        className="flex items-center gap-3 rounded-lg border border-border bg-card px-3 py-2"
                      >
                        <div className="h-7 w-7 rounded-full bg-muted flex items-center justify-center shrink-0">
                          <span className="text-xs font-medium text-foreground">
                            {op.firstName[0]}{op.lastName[0]}
                          </span>
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-foreground truncate">
                            {op.firstName} {op.lastName}
                          </p>
                          <p className="text-xs text-muted-foreground truncate">{op.email}</p>
                        </div>
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7 text-muted-foreground hover:text-destructive shrink-0"
                          onClick={() => onRevoke(selectedMissionId, op.id)}
                          title="Revoke operator (RN-16, RN-25)"
                        >
                          <XIcon className="h-3.5 w-3.5" />
                          <span className="sr-only">Revoke</span>
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Available operators */}
              <div className="space-y-2">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Available to Assign ({unassignedOperators.length})
                </p>
                {unassignedOperators.length === 0 ? (
                  <p className="text-xs text-muted-foreground italic">All active operators are already assigned.</p>
                ) : (
                  <div className="space-y-1.5">
                    {unassignedOperators.map((op) => (
                      <div
                        key={op.id}
                        className="flex items-center gap-3 rounded-lg border border-dashed border-border bg-muted/20 px-3 py-2"
                      >
                        <div className="h-7 w-7 rounded-full bg-muted flex items-center justify-center shrink-0">
                          <span className="text-xs font-medium text-muted-foreground">
                            {op.firstName[0]}{op.lastName[0]}
                          </span>
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm text-foreground truncate">
                            {op.firstName} {op.lastName}
                          </p>
                          <p className="text-xs text-muted-foreground truncate">{op.email}</p>
                        </div>
                        <Button
                          size="sm"
                          variant="outline"
                          className="h-7 text-xs gap-1 shrink-0"
                          onClick={() => onAssign(selectedMissionId, op.id)}
                        >
                          <PlusIcon className="h-3 w-3" />
                          Assign
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </SheetContent>
    </Sheet>
  );
}

// ─── Main Operator Management View ────────────────────────────────────────────

export function OperatorManagement({
  operators,
  missions,
  onOperatorsChange,
  onMissionsChange,
}: OperatorManagementProps) {
  const [createOpen, setCreateOpen] = useState(false);
  const [assignOpen, setAssignOpen] = useState(false);
  const [deactivateTarget, setDeactivateTarget] = useState<Operator | null>(null);

  const handleCreate = (data: CreateOperatorPayload) => {
    const existing = operators.find((o) => o.email === data.email);
    if (existing) return; // Keycloak uniqueness guard
    const newOp: Operator = {
      id: `op-${Date.now()}`,
      ...data,
      status: "Active",
      assignedMissions: [],
    };
    onOperatorsChange([...operators, newOp]);
  };

  const handleDeactivate = () => {
    if (!deactivateTarget) return;
    // RN-26: Would check for active sessions in real implementation
    onOperatorsChange(
      operators.map((o) =>
        o.id === deactivateTarget.id ? { ...o, status: "Inactive" } : o
      )
    );
    setDeactivateTarget(null);
  };

  const handleAssign = (missionId: string, operatorId: string) => {
    // Update mission assigned operators (RN-24)
    onMissionsChange(
      missions.map((m) =>
        m.id === missionId
          ? { ...m, assignedOperators: [...(m.assignedOperators ?? []), { operatorId }] }
          : m
      )
    );
    // Update operator assigned missions
    onOperatorsChange(
      operators.map((o) =>
        o.id === operatorId
          ? { ...o, assignedMissions: [...(o.assignedMissions ?? []), missionId] }
          : o
      )
    );
  };

  const handleRevoke = (missionId: string, operatorId: string) => {
    // RN-25: Would check active live sessions in real implementation
    onMissionsChange(
      missions.map((m) =>
        m.id === missionId
          ? { ...m, assignedOperators: (m.assignedOperators ?? []).filter((r) => r.operatorId !== operatorId) }
          : m
      )
    );
    onOperatorsChange(
      operators.map((o) =>
        o.id === operatorId
          ? { ...o, assignedMissions: (o.assignedMissions ?? []).filter((id) => id !== missionId) }
          : o
      )
    );
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-foreground text-balance">Operator Management</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {operators.filter((o) => o.status === "Active").length} active · {operators.filter((o) => o.status === "Inactive").length} inactive
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setAssignOpen(true)}>
            <ShieldIcon className="h-4 w-4" />
            Assign to Mission
          </Button>
          <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
            <PlusIcon className="h-4 w-4" />
            New Operator
          </Button>
        </div>
      </div>

      {/* Table */}
      <div className="rounded-lg border border-border bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted/40 hover:bg-muted/40">
              <TableHead className="font-medium text-foreground">Operator</TableHead>
              <TableHead className="font-medium text-foreground">Email</TableHead>
              <TableHead className="font-medium text-foreground">Status</TableHead>
              <TableHead className="font-medium text-foreground">Missions Assigned</TableHead>
              <TableHead className="font-medium text-foreground text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {operators.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center py-12 text-muted-foreground text-sm">
                  No operators registered yet.
                </TableCell>
              </TableRow>
            ) : (
              operators.map((op) => {
                const assignedMissions = op.assignedMissions
                  ?.map((id) => missions.find((m) => m.id === id)?.title)
                  .filter(Boolean);

                return (
                  <TableRow key={op.id} className="hover:bg-muted/30 transition-colors">
                    <TableCell>
                      <div className="flex items-center gap-2.5">
                        <div className="h-7 w-7 rounded-full bg-muted flex items-center justify-center shrink-0">
                          <span className="text-xs font-medium text-foreground">
                            {op.firstName[0]}{op.lastName[0]}
                          </span>
                        </div>
                        <span className="text-sm font-medium text-foreground">
                          {op.firstName} {op.lastName}
                        </span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">{op.email}</span>
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={op.status} />
                    </TableCell>
                    <TableCell>
                      {assignedMissions && assignedMissions.length > 0 ? (
                        <div className="flex flex-wrap gap-1">
                          {assignedMissions.slice(0, 2).map((title) => (
                            <span
                              key={title}
                              className="text-xs bg-muted text-muted-foreground px-2 py-0.5 rounded-full"
                            >
                              {title}
                            </span>
                          ))}
                          {assignedMissions.length > 2 && (
                            <span className="text-xs text-muted-foreground">
                              +{assignedMissions.length - 2} more
                            </span>
                          )}
                        </div>
                      ) : (
                        <span className="text-sm text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {op.status === "Active" ? (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 text-xs gap-1 text-muted-foreground hover:text-destructive"
                          onClick={() => setDeactivateTarget(op)}
                        >
                          <UserXIcon className="h-3.5 w-3.5" />
                          Deactivate
                        </Button>
                      ) : (
                        <span className="text-xs text-muted-foreground italic">Inactive</span>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      <CreateOperatorModal open={createOpen} onClose={() => setCreateOpen(false)} onSubmit={handleCreate} />

      <AssignOperatorSheet
        open={assignOpen}
        onClose={() => setAssignOpen(false)}
        missions={missions}
        operators={operators}
        onAssign={handleAssign}
        onRevoke={handleRevoke}
      />

      <AlertDialog open={!!deactivateTarget} onOpenChange={(v) => !v && setDeactivateTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Deactivate Operator</AlertDialogTitle>
            <AlertDialogDescription>
              This will globally deactivate <strong>{deactivateTarget?.firstName} {deactivateTarget?.lastName}</strong> in Keycloak.
              Per <strong>RN-26</strong>, this action is blocked if the operator has any active sessions in the system.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeactivate}
              className="bg-destructive text-white hover:bg-destructive/90"
            >
              Deactivate
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
