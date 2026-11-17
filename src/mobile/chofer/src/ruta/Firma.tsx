import { useMemo, useRef, useState } from 'react';
import { PanResponder, Text, View, type LayoutChangeEvent } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import { radius, space, useTheme } from '@cleancom/mobile-shared';

export type Signature = { width: number; height: number; strokes: number[][] };

/**
 * Signature pad: the invitado signs with a finger. Only the strokes (numbers) are sent; the API draws the image.
 */
export function Firma({ onChange }: { onChange: (s: Signature | null) => void }) {
  const { colors } = useTheme();
  const [size, setSize] = useState({ width: 0, height: 0 });
  const [strokes, setStrokes] = useState<number[][]>([]);
  const current = useRef<number[] | null>(null);
  const sizeRef = useRef(size);
  sizeRef.current = size;

  const publish = (next: number[][]) => {
    setStrokes(next);
    const { width, height } = sizeRef.current;
    onChange(next.some((s) => s.length >= 4) ? { width, height, strokes: next } : null);
  };

  const responder = useMemo(
    () =>
      PanResponder.create({
        onStartShouldSetPanResponder: () => true,
        onMoveShouldSetPanResponder: () => true,
        // Keeps the scroll view from taking the gesture while signing.
        onPanResponderTerminationRequest: () => false,
        onPanResponderGrant: (e) => {
          const { locationX, locationY } = e.nativeEvent;
          current.current = [round(locationX), round(locationY)];
          setStrokes((s) => [...s, current.current!]);
        },
        onPanResponderMove: (e) => {
          const { locationX, locationY } = e.nativeEvent;
          if (!current.current) return;
          current.current.push(round(locationX), round(locationY));
          setStrokes((s) => [...s.slice(0, -1), [...current.current!]]);
        },
        onPanResponderRelease: () => {
          current.current = null;
          setStrokes((s) => {
            publishLater(s);
            return s;
          });
        },
      }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  );

  // Publishing inside setState's updater would update the parent during render.
  const publishLater = (s: number[][]) => setTimeout(() => publish(s), 0);

  return (
    <View style={{ gap: space.xs }}>
      <View
        onLayout={(e: LayoutChangeEvent) => setSize({ width: round(e.nativeEvent.layout.width), height: round(e.nativeEvent.layout.height) })}
        style={{ height: 180, borderRadius: radius.md, borderWidth: 1, borderColor: colors.border, backgroundColor: '#ffffff', overflow: 'hidden' }}
        {...responder.panHandlers}
      >
        <Svg width="100%" height="100%">
          {strokes.map((s, i) => (
            <Path key={i} d={toPath(s)} stroke="#111111" strokeWidth={3} fill="none" strokeLinecap="round" strokeLinejoin="round" />
          ))}
        </Svg>
        {strokes.length === 0 && (
          <Text pointerEvents="none" style={{ position: 'absolute', bottom: space.sm, alignSelf: 'center', color: '#98a3b8' }}>
            Firma del invitado aquí
          </Text>
        )}
      </View>
      {strokes.length > 0 && (
        <Text onPress={() => publish([])} style={{ color: colors.brand, fontWeight: '600', alignSelf: 'flex-end', padding: space.xs }}>
          Borrar firma
        </Text>
      )}
    </View>
  );
}

const round = (n: number) => Math.round(n * 10) / 10;

function toPath(s: number[]) {
  let d = '';
  for (let i = 0; i + 1 < s.length; i += 2) d += `${i === 0 ? 'M' : 'L'}${s[i]} ${s[i + 1]} `;
  // A single tap still shows a dot.
  if (s.length === 2) d += `L${s[0] + 0.5} ${s[1] + 0.5}`;
  return d;
}
