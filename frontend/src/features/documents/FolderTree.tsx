import { ChevronRight, Folder as FolderIcon, FolderOpen } from 'lucide-react';
import type { Folder } from '../../types';

interface FolderTreeProps {
  folders: Folder[];
  selectedId: string | null;
  onSelect: (folder: Folder) => void;
}

export function FolderTree({ folders, selectedId, onSelect }: FolderTreeProps) {
  return (
    <div className="folder-tree">
      {folders.map((folder) => (
        <FolderNode key={folder.id} folder={folder} selectedId={selectedId} onSelect={onSelect} depth={0} />
      ))}
    </div>
  );
}

interface FolderNodeProps {
  folder: Folder;
  selectedId: string | null;
  onSelect: (folder: Folder) => void;
  depth: number;
}

function FolderNode({ folder, selectedId, onSelect, depth }: FolderNodeProps) {
  const selected = selectedId === folder.id;
  const hasChildren = folder.children.length > 0;

  return (
    <div>
      <button
        type="button"
        className={`tree-node ${selected ? 'selected' : ''}`}
        style={{ paddingLeft: 12 + depth * 16 }}
        onClick={() => onSelect(folder)}
      >
        {hasChildren ? <ChevronRight size={14} /> : <span className="tree-spacer" />}
        {selected ? <FolderOpen size={16} /> : <FolderIcon size={16} />}
        <span>{folder.name}</span>
      </button>
      {folder.children.map((child) => (
        <FolderNode key={child.id} folder={child} selectedId={selectedId} onSelect={onSelect} depth={depth + 1} />
      ))}
    </div>
  );
}
