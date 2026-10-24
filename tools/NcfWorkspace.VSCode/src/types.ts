export interface PairingCreateResponse {
  requestId: string;
  deviceCode: string;
  pollSecret: string;
  expiresAt: string;
  verificationPath: string;
  pollIntervalSeconds: number;
}

export interface PairingPollResponse {
  status: string;
  sessionToken?: string;
  sessionExpiresAt?: string;
  message?: string;
}

export interface AdminHandoffCreateResponse {
  requestId: string;
  expiresAt: string;
  approvalPath: string;
  pollIntervalMilliseconds: number;
}

export interface AdminHandoffRedeemResponse {
  status: string;
  userName?: string;
  accessToken?: string;
  expiresUtc?: string;
  message?: string;
}

export interface WorkspaceCapabilities {
  protocolVersion: number;
  runtimeProvider: string;
  isolationLevel: string;
  executionEnabled: boolean;
  maxReadBytes: number;
  maxWriteBytes: number;
  supportsBinaryFiles: boolean;
  supportsEtags: boolean;
  supportsEvents: boolean;
}

export interface WorkspaceDescriptor {
  id: string;
  name: string;
  createdUtc: string;
  lastAccessedUtc: string;
  storageProvider: string;
}

export interface WorkspaceEntry {
  relativePath: string;
  isDirectory: boolean;
  length: number;
  lastWriteTimeUtc: string;
}

export interface WorkspaceFileMetadata {
  relativePath: string;
  isDirectory: boolean;
  length: number;
  creationTimeUtc: string;
  lastWriteTimeUtc: string;
  eTag: string;
}

export interface WorkspaceWriteFileResult {
  metadata: WorkspaceFileMetadata;
  created: boolean;
}

export interface WorkspaceChangeEvent {
  sequence: number;
  workspaceId: string;
  relativePath: string;
  changeType: "created" | "changed" | "deleted" | "renamed" | string;
  time: string;
  previousRelativePath?: string;
  eTag?: string;
}
