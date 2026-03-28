import { Avatar, AvatarGroup, Tooltip } from '@mui/material';
import type { User } from '../../types/user';

interface UserAvatarGroupProps {
  users: User[];
  max?: number;
  size?: number;
}

export const UserAvatarGroup = ({ users, max = 4, size = 36 }: UserAvatarGroupProps) => (
  <AvatarGroup
    max={max}
    sx={{
      justifyContent: 'flex-end',
      '& .MuiAvatar-root': {
        width: size,
        height: size,
        fontSize: size * 0.38,
        bgcolor: '#dce8ff',
        color: '#1f4c8f',
        border: '2px solid white',
      },
    }}
  >
    {users.map((user) => (
      <Tooltip key={user.id} title={`${user.name} • ${user.email}`}>
        <Avatar>{user.avatar}</Avatar>
      </Tooltip>
    ))}
  </AvatarGroup>
);
