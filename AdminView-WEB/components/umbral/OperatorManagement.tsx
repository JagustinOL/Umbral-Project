"use client";

import { FormEvent, useState } from "react";
import {
  PlusIcon,
  UserXIcon,
  ShieldIcon,
  XIcon,
  Loader2Icon,
  AlertTriangleIcon,
  MailIcon,
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
  DialogDescription,
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
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { StatusBadge } from "./StatusBadge";
import { Operator, Mission, CreateOperatorPayload } from "@/lib/types";
import { CreateOperatorResponse } from "@/lib/types/api";
import { cn } from "@/lib/utils";

interface OperatorManagementProps {
  operators: Operator[];
  missions: Mission[];
  onCreateOperator: (payload: CreateOperatorPayload) => Promise<CreateOperatorResponse>;
  onResendActivation: (operatorId: string) => Promise<CreateOperatorResponse>;
  onDeactivateOperator: (operatorId: string) => Promise<void>;
  onAssignOperator: (missionId: string, operatorId: string) => Promise<void>;
  onRevokeOperator: (missionId: string, operatorId: string) => Promise<void>;
  isLoading: boolean;
  isCreating: boolean;
  isDeactivating: boolean;
  isAssigning: boolean;
  isRevoking: boolean;
  errorMessage: string | null;
  assignmentError: string | null;
  onRetry: () => void;
  onClearAssignmentError: () => void;
}

// ─── Create Operator Modal ─────────────────────────────────────────────────────

interface CreateOperatorModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateOperatorPayload) => Promise<CreateOperatorResponse>;
  isSubmitting: boolean;
  onCreated: (result: CreateOperatorResponse) => void;
}

function CreateOperatorModal({ open, onClose, onSubmit, isSubmitting, onCreated }: CreateOperatorModalProps) {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      const result = await onSubmit({ firstName, lastName, email });
      setFirstName("");
      setLastName("");
      setEmail("");
      onClose();
      onCreated(result);
    } catch {
      // Error is handled at page-level and rendered as alert.
    }
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">Create Operator Account</DialogTitle>
          <DialogDescription className="text-sm text-muted-foreground">
            Create a pending operator account. The activation code is sent only to their email — you will not see it.
            If the email already belongs to an inactive operator, a new code is sent so they can activate again.
          </DialogDescription>
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
            The account is created inactive in Keycloak. The operator activates it with the one-time code received by email.
          </p>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>Cancel</Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Creating..." : "Create Account"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

interface ActivationEmailDialogProps {
  open: boolean;
  email: string | null;
  emailSent: boolean;
  onClose: () => void;
}

function ActivationEmailDialog({ open, email, emailSent, onClose }: ActivationEmailDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-base font-semibold">
            {emailSent ? "Activation email sent" : "Operator created"}
          </DialogTitle>
          <DialogDescription className="text-sm text-muted-foreground">
            {emailSent
              ? `The activation code was sent to ${email}. The operator must open Activate account on the login screen.`
              : `The operator account was created for ${email}, but the email could not be sent. Use Resend activation from the list.`}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button type="button" onClick={onClose}>Done</Button>
        </DialogFooter>
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
  onAssign: (missionId: string, operatorId: string) => Promise<void>;
  onRevoke: (missionId: string, operatorId: string) => Promise<void>;
  isAssigning: boolean;
  isRevoking: boolean;
  assignmentError: string | null;
  onClearAssignmentError: () => void;
}

function AssignOperatorSheet({
  open,
  onClose,
  missions,
  operators,
  onAssign,
  onRevoke,
  isAssigning,
  isRevoking,
  assignmentError,
  onClearAssignmentError,
}: AssignOperatorSheetProps) {
  const [selectedMissionId, setSelectedMissionId] = useState<string>("");
  const selectedMission = missions.find((m) => m.id === selectedMissionId);
  const assigned = selectedMission?.assignedOperators ?? [];
  const isBusy = isAssigning || isRevoking;

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
          <SheetTitle className="text-base font-semibold">Asignar operadores a misión</SheetTitle>
        </SheetHeader>
        <div className="mt-6 space-y-5">
          {assignmentError && (
            <Alert variant="destructive">
              <AlertTriangleIcon className="h-4 w-4" />
              <AlertTitle>Error de asignación</AlertTitle>
              <AlertDescription className="flex items-center justify-between gap-2">
                <span>{assignmentError}</span>
                <Button variant="outline" size="sm" onClick={onClearAssignmentError}>
                  Cerrar
                </Button>
              </AlertDescription>
            </Alert>
          )}
          <div className="space-y-1.5">
            <Label>Seleccionar misión</Label>
            <Select
              value={selectedMissionId}
              onValueChange={(value) => {
                onClearAssignmentError();
                setSelectedMissionId(value);
              }}
            >
              <SelectTrigger className="text-sm">
                <SelectValue placeholder="Choose a mission…" />
              </SelectTrigger>
              <SelectContent>
                {missions.filter((m) => m.status !== "Inactive").map((m) => (
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
                  Asignados ({assignedOperators.length})
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
                          disabled={isBusy}
                          onClick={() => void onRevoke(selectedMissionId, op.id)}
                          title="Revocar operador"
                        >
                          {isRevoking ? (
                            <Loader2Icon className="h-3.5 w-3.5 animate-spin" />
                          ) : (
                            <XIcon className="h-3.5 w-3.5" />
                          )}
                          <span className="sr-only">Revocar</span>
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Available operators */}
              <div className="space-y-2">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Disponibles ({unassignedOperators.length})
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
                          disabled={isBusy}
                          onClick={() => void onAssign(selectedMissionId, op.id)}
                        >
                          {isAssigning ? (
                            <Loader2Icon className="h-3 w-3 animate-spin" />
                          ) : (
                            <PlusIcon className="h-3 w-3" />
                          )}
                          Asignar
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
  onCreateOperator,
  onResendActivation,
  onDeactivateOperator,
  onAssignOperator,
  onRevokeOperator,
  isLoading,
  isCreating,
  isDeactivating,
  isAssigning,
  isRevoking,
  errorMessage,
  assignmentError,
  onRetry,
  onClearAssignmentError,
}: OperatorManagementProps) {
  const [createOpen, setCreateOpen] = useState(false);
  const [assignOpen, setAssignOpen] = useState(false);
  const [deactivateTarget, setDeactivateTarget] = useState<Operator | null>(null);
  const [activationNotice, setActivationNotice] = useState<{
    email: string;
    emailSent: boolean;
  } | null>(null);
  const [resendingId, setResendingId] = useState<string | null>(null);

  const handleCreate = async (data: CreateOperatorPayload) => onCreateOperator(data);

  const handleResend = async (operatorId: string) => {
    setResendingId(operatorId);
    try {
      const result = await onResendActivation(operatorId);
      setActivationNotice({ email: result.email, emailSent: result.activationEmailSent });
    } catch {
      // Error is handled at page-level and rendered as alert.
    } finally {
      setResendingId(null);
    }
  };

  const handleDeactivate = async () => {
    if (!deactivateTarget) return;
    try {
      await onDeactivateOperator(deactivateTarget.id);
      setDeactivateTarget(null);
    } catch {
      // Error is handled at page-level and rendered as alert.
    }
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-foreground text-balance">Operator Management</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {operators.length} active operator{operators.length !== 1 ? "s" : ""}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setAssignOpen(true)}>
            <ShieldIcon className="h-4 w-4" />
            Asignar/Revocar misión
          </Button>
          <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
            <PlusIcon className="h-4 w-4" />
            New Operator
          </Button>
        </div>
      </div>

      {/* Table */}
      <div className="rounded-lg border border-border bg-card overflow-hidden">
        {errorMessage && (
          <Alert variant="destructive" className="m-3 mb-0">
            <AlertTitle>Operator API error</AlertTitle>
            <AlertDescription className="flex items-center justify-between gap-2">
              <span>{errorMessage}</span>
              <Button variant="outline" size="sm" onClick={onRetry}>
                Retry
              </Button>
            </AlertDescription>
          </Alert>
        )}
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
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center py-12 text-muted-foreground text-sm">
                  Loading operators...
                </TableCell>
              </TableRow>
            ) : operators.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center py-12 text-muted-foreground text-sm">
                  No operators registered yet.
                </TableCell>
              </TableRow>
            ) : (
              operators.map((op) => {
                const assignedMissions = missions
                  .filter((m) =>
                    (m.assignedOperators ?? []).some((assigned) => assigned.operatorId === op.id),
                  )
                  .map((m) => m.title);

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
                          disabled={isDeactivating}
                        >
                          <UserXIcon className="h-3.5 w-3.5" />
                          Deactivate
                        </Button>
                      ) : (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 text-xs gap-1"
                          onClick={() => void handleResend(op.id)}
                          disabled={resendingId === op.id}
                        >
                          {resendingId === op.id ? (
                            <Loader2Icon className="h-3.5 w-3.5 animate-spin" />
                          ) : (
                            <MailIcon className="h-3.5 w-3.5" />
                          )}
                          Resend activation
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      <CreateOperatorModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSubmit={handleCreate}
        isSubmitting={isCreating}
        onCreated={(result) =>
          setActivationNotice({ email: result.email, emailSent: result.activationEmailSent })
        }
      />

      <ActivationEmailDialog
        open={!!activationNotice}
        email={activationNotice?.email ?? null}
        emailSent={activationNotice?.emailSent ?? false}
        onClose={() => setActivationNotice(null)}
      />

      <AssignOperatorSheet
        open={assignOpen}
        onClose={() => {
          onClearAssignmentError();
          setAssignOpen(false);
        }}
        missions={missions}
        operators={operators}
        onAssign={onAssignOperator}
        onRevoke={onRevokeOperator}
        isAssigning={isAssigning}
        isRevoking={isRevoking}
        assignmentError={assignmentError}
        onClearAssignmentError={onClearAssignmentError}
      />

      <AlertDialog open={!!deactivateTarget} onOpenChange={(v) => !v && setDeactivateTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Deactivate Operator</AlertDialogTitle>
            <AlertDialogDescription>
              This will globally deactivate <strong>{deactivateTarget?.firstName} {deactivateTarget?.lastName}</strong> in Keycloak.
              This action is blocked if the operator has any active sessions in the system.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => void handleDeactivate()}
              className="bg-destructive text-white hover:bg-destructive/90"
              disabled={isDeactivating}
            >
              {isDeactivating ? "Deactivating..." : "Deactivate"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
