"use client";

import * as React from "react";
import { ChevronLeft, ChevronRight, MoreHorizontal } from "lucide-react";
import { Button } from "./button";
import { cn } from "@/lib/utils";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  className?: string;
  siblingCount?: number;
}

function range(start: number, end: number): number[] {
  return Array.from({ length: end - start + 1 }, (_, i) => start + i);
}

function usePaginationRange(
  currentPage: number,
  totalPages: number,
  siblingCount: number
): (number | "dots")[] {
  const totalNumbers = siblingCount * 2 + 5;

  if (totalNumbers >= totalPages) {
    return range(1, totalPages);
  }

  const leftSiblingIndex = Math.max(currentPage - siblingCount, 1);
  const rightSiblingIndex = Math.min(currentPage + siblingCount, totalPages);

  const showLeftDots = leftSiblingIndex > 2;
  const showRightDots = rightSiblingIndex < totalPages - 1;

  if (!showLeftDots && showRightDots) {
    const leftItemCount = 3 + 2 * siblingCount;
    const leftRange = range(1, leftItemCount);
    return [...leftRange, "dots", totalPages];
  }

  if (showLeftDots && !showRightDots) {
    const rightItemCount = 3 + 2 * siblingCount;
    const rightRange = range(totalPages - rightItemCount + 1, totalPages);
    return [1, "dots", ...rightRange];
  }

  const middleRange = range(leftSiblingIndex, rightSiblingIndex);
  return [1, "dots", ...middleRange, "dots", totalPages];
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  className,
  siblingCount = 1,
}: PaginationProps) {
  const paginationRange = usePaginationRange(currentPage, totalPages, siblingCount);

  if (totalPages <= 1) return null;

  return (
    <nav
      role="navigation"
      aria-label="Pagination"
      className={cn("flex items-center justify-center gap-1", className)}
    >
      <Button
        variant="outline"
        size="icon"
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage <= 1}
        aria-label="Previous page"
        className="h-9 w-9"
      >
        <ChevronLeft className="h-4 w-4" />
      </Button>

      {paginationRange.map((page, idx) =>
        page === "dots" ? (
          <span
            key={`dots-${idx}`}
            className="flex h-9 w-9 items-center justify-center"
            aria-hidden="true"
          >
            <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
          </span>
        ) : (
          <Button
            key={page}
            variant={page === currentPage ? "primary" : "outline"}
            size="icon"
            onClick={() => onPageChange(page)}
            aria-label={`Page ${page}`}
            aria-current={page === currentPage ? "page" : undefined}
            className="h-9 w-9"
          >
            {page}
          </Button>
        )
      )}

      <Button
        variant="outline"
        size="icon"
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage >= totalPages}
        aria-label="Next page"
        className="h-9 w-9"
      >
        <ChevronRight className="h-4 w-4" />
      </Button>
    </nav>
  );
}
