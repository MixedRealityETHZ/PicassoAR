def format_bytes(size):
    """
    Converts a size in bytes to the nearest unit (B, kB, MB, GB).

    Args:
        size (int): Size in bytes.

    Returns:
        str: Formatted size with the appropriate unit.
    """
    units = ["B", "kB", "MB", "GB"]
    index = 0
    while size >= 1024 and index < len(units) - 1:
        size /= 1024.0
        index += 1
    return f"{size:.2f} {units[index]}"