-- database/schema.sql
create table public.leaderboard (
  id bigint generated always as identity primary key,
  player_name text not null check (char_length(player_name) between 1 and 20),
  score integer not null default 0 check (score >= 0),
  burgers_served integer not null default 0 check (burgers_served >= 0),
  deleted_at timestamptz,                          -- for soft delete
  created_at timestamptz not null default now()
);

create index on public.leaderboard (score desc);   -- habit 1: fast top-10 lookups

grant select, insert, update on public.leaderboard to anon;  -- lock 1
alter table public.leaderboard enable row level security;    -- lock 2

create policy "read_all"     on public.leaderboard for select to anon using (true);
create policy "insert_score" on public.leaderboard for insert to anon with check (deleted_at is null);
create policy "update_all"   on public.leaderboard for update to anon using (true) with check (true);