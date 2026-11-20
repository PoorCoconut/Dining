# Dining
Diner Rush Game where you make burgers!!!

Play the Game in your browser!
Phone and PC supported!
https://poorcoconut.github.io/Dining/


HOW TO PLAY:
The game only takes 60 seconds.
Customers will appear and take a table. They will display how they want their burgers structured!
Each customer has a certain timer.
It is your job to follow the order of the ingredients they want.
Making a customer happy will increase your time for +1 second, otherwise they will just leave.
Burgers are made bottom to top. Click on the ingredients at the bottom of your screen!
Click on a customer to SERVE their burgers!
Try to get a high score at the end of the day!

## Storage pattern: soft delete

RESTORUSH stores its leaderboard in a Supabase (PostgreSQL) table called `leaderboard`.
Rows are never removed. Deleting a score only fills in the `deleted_at` column
with the current time, and every query that shows scores filters on
`deleted_at IS NULL` (`.Where(x => x.DeletedAt == null)`).

### Why soft delete?
- **Mistakes are recoverable.** A wrongly deleted score can be restored by
  clearing `deleted_at` in the Table Editor.
- **Cheaters can be hidden without losing evidence.** A suspicious score is
  removed from the leaderboard but stays in the database for review.
- **History is kept.** The data survives page refreshes and app restarts
  because it lives in the database, not in the browser.

### Other storage decisions
- The `anon` role has `select`, `insert` and `update` permissions only. It has
  no `delete` permission, so nobody can wipe the table from the browser.
- An index on `score` (descending) keeps the Top 10 query fast.
- Row Level Security is enabled with open policies. This is for class use only.
- `database/schema.sql` contains the full table and policy definitions.